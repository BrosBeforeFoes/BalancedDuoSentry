using HarmonyLib;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;
using System;

// Corrected using directives for types present in Assembly-CSharp.dll
using CG.Game;
using CG.Client.Utils;
using CG.Objects;
using CG.Ship.Hull;
using CG.Space;
using Gameplay.Utilities;
using Gameplay.CompositeWeapons;
using ToolClasses;
using CG.Ship.Modules; // For CentralShipComputerModule


namespace FairDuoSentry
{

    // --- Patch 1: Extend SinglePlayerModRule to include 2 players ---
    [HarmonyPatch(typeof(SinglePlayerModRule), nameof(SinglePlayerModRule.ShouldApply))]
    internal class SinglePlayerModRulePatch
    {
        static bool Prefix(ref bool __result)
        {
            int playerCount = RoomPlayersTracker.Instance.Players.Count;
            __result = (playerCount == 1 || playerCount == 2);
            BepinPlugin.Log.LogInfo($"SinglePlayerModRule.ShouldApply patched. Player count: {playerCount}, Returning: {__result}");
            
            return false; // Skip original method
        }
    }

    // --- NEW TARGET PATCH: Dynamically Adjust StatMod values via ModSocket.ApplyMods ---
    [HarmonyPatch(typeof(ModSocket), nameof(ModSocket.ApplyMods))]
    internal class ModSocket_ApplyMods_Patch
    {
        // Use Postfix to run after original mods are applied by the socket
        static void Postfix(ModSocket __instance, List<StatMod> mods) // 'mods' is the list being applied by this socket
        {
            if (!PhotonNetwork.IsMasterClient) return;

            // Step 1: Identify if this ModSocket is the HomunculusSocket on the Sentry Frigate's Central Computer Module
            // We need to find the CentralShipComputerModule of the player's current ship.
            CentralShipComputerModule centralComputer = ClientGame.Current?.PlayerShip?.GetModule<CentralShipComputerModule>();
            
            if (centralComputer == null || __instance != centralComputer.HomunculusSocket)
            {
                // This is not the HomunculusSocket we care about.
                return;
            }

            int playerCount = RoomPlayersTracker.Instance.Players.Count;
            
            BepinPlugin.Log.LogInfo($"[DEBUG - ModSocket.ApplyMods] Checking {mods.Count} mods in list...");

            // Iterate through the list of mods *being applied by this socket*
            foreach (StatMod currentModBeingApplied in mods)
            {
                string statName = StatType.GetNameById(currentModBeingApplied.Type, false);
                string dynamicConditionType = currentModBeingApplied.DynamicCondition?.GetType().Name ?? "NULL";
                string modPrimitiveType = currentModBeingApplied.Mod?.GetType().Name ?? "NULL";
                string modAmount = "N/A";

                if (currentModBeingApplied.Mod is PrimitiveModifier<float> floatMod) modAmount = floatMod.Amount.ToString();
                else if (currentModBeingApplied.Mod is PrimitiveModifier<int> intMod) modAmount = intMod.Amount.ToString();

                BepinPlugin.Log.LogInfo($"- Processing Mod from Socket: Name={statName}, TypeID={currentModBeingApplied.Type}, Condition={dynamicConditionType}, ModPrimitive={modPrimitiveType}, Amount={modAmount}");


                // Check if this StatMod's dynamic condition is our SinglePlayerModRule
                if (currentModBeingApplied.DynamicCondition is SinglePlayerModRule)
                {
                    BepinPlugin.Log.LogInfo($"- Confirmed DynamicCondition is SinglePlayerModRule for Stat: {statName} (from ModSocket)");

                    // PowerWanted modification (Id 271581185)
                    if (currentModBeingApplied.Type == StatType.PowerWanted.Id)
                    {
                        if (currentModBeingApplied.Mod is PrimitiveModifier<int> powerIntMod)
                        {
                            int newPowerWantedValue = 0; // Default for 3+ players or no special effect
                            if (playerCount == 1)
                            {
                                newPowerWantedValue = -3; // Solo: Power Consumption: -2
                            }
                            else if (playerCount == 2)
                            {
                                newPowerWantedValue = -1; // Duo: Power Consumption: -1
                            }

                            if (playerCount == 1 || playerCount == 2)
                            {
                                powerIntMod.Amount = newPowerWantedValue;
                                currentModBeingApplied.DynamicCondition = null; // Nullify condition, as our patch now controls its value
                                BepinPlugin.Log.LogInfo($"Modified PowerWanted to: {powerIntMod.Amount} (int) for {playerCount} players (via ModSocket).");
                            }
                        } else {
                            BepinPlugin.Log.LogWarning($"- PowerWanted mod from socket.Mod is NOT PrimitiveModifier<int>. Actual type: {modPrimitiveType}");
                        }
                    }
                    // Damage modification (Id 1)
                    else if (currentModBeingApplied.Type == StatType.Damage.Id)
                    {
                        if (currentModBeingApplied.Mod is PrimitiveModifier<float> damageFloatMod)
                        {
                            float newDamageValue = 0f;
                            // Reconfirmed based on solo effect +100% being a multiplicative factor.
                            // If base damage is D, +100% means D + D*1.0 = 2D.
                            // +75% means D + D*0.75 = 1.75D.
                            
                            if (playerCount == 1)
                            {
                                newDamageValue = 2.0f; // Solo: Damage: +100% multiplier (value is 1.0 for 100% increase)
                            }
                            else if (playerCount == 2)
                            {
                                newDamageValue = 0.75f; // Duo: Damage: +75% multiplier (value is 0.75 for 75% increase)
                            }

                            if (playerCount == 1 || playerCount == 2)
                            {
                                damageFloatMod.Amount = newDamageValue;
                                currentModBeingApplied.DynamicCondition = null; // Nullify condition
                                BepinPlugin.Log.LogInfo($"Modified Damage to: {damageFloatMod.Amount} (float multiplier) for {playerCount} players (via ModSocket).");
                            }
                        } else {
                            BepinPlugin.Log.LogWarning($"- Damage mod from socket.Mod is NOT PrimitiveModifier<float>. Actual type: {modPrimitiveType}");
                        }
                    }
                }
            }

        }
    }


    // --- ORIGINAL LOGGER PATCHES (Keep these for general debugging) ---
    // CompositeWeaponModuleLoggerPatch (useful for getting weapon GUID and base stats)
    [HarmonyPatch(typeof(CompositeWeaponModule), nameof(CompositeWeaponModule.OnPhotonInstantiate))]
    public static class CompositeWeaponModuleLoggerPatch
    {
        public static void Postfix(CompositeWeaponModule __instance)
        {
            if (__instance == null || !__instance.photonView.IsMine) return;

            if (__instance.CompositeDataRef != null && !__instance.CompositeDataRef.IsNull)
            {
                BepinPlugin.Log.LogInfo($"[Weapon Module Logger] Composite Weapon Module '{__instance.DisplayName}' instantiated.");
                BepinPlugin.Log.LogInfo($"[Weapon Module Logger] Weapon GUID: {__instance.CompositeDataRef.AssetGuid.ToString()}");
                
                if (__instance.Stats != null)
                {
                    string powerWantedBaseValue = "N/A";
                    if (__instance.Stats.Stats.TryGetValue(StatType.PowerWanted.Id, out var pWStat))
                    {
                        if (pWStat.Data is ModifiableFloat floatData) { powerWantedBaseValue = floatData.BaseValue.ToString(); }
                        else if (pWStat.Data is ModifiableInt intData) { powerWantedBaseValue = intData.BaseValue.ToString(); }
                        else if (pWStat.Data is PipModifiableFloat pipFloatData) { powerWantedBaseValue = pipFloatData.BaseValue.ToString(); }
                        BepinPlugin.Log.LogInfo($"[Weapon Module Logger] Base PowerWanted (Id {StatType.PowerWanted.Id}): {powerWantedBaseValue} (Type: {pWStat.Data?.GetType().Name ?? "null"})");
                    }

                    string damageBaseValue = "N/A";
                    if (__instance.Stats.Stats.TryGetValue(StatType.Damage.Id, out var damageStat))
                    {
                        if (damageStat.Data is ModifiableFloat damageFloatData) { damageBaseValue = damageFloatData.BaseValue.ToString(); }
                        else if (damageStat.Data is ModifiableInt intData) { damageBaseValue = intData.BaseValue.ToString(); }
                        else if (damageStat.Data is PipModifiableFloat pipFloatData) { damageBaseValue = pipFloatData.BaseValue.ToString(); }
                        BepinPlugin.Log.LogInfo($"[Weapon Module Logger] Base Damage (Id {StatType.Damage.Id}): {damageBaseValue} (Type: {damageStat.Data?.GetType().Name ?? "null"})");
                    }
                }
            }
        }
    }

    // HomunculusDispenseLoggerPatch (useful for general debugging)
    [HarmonyPatch(typeof(HomunculusAndBiomassSocket), nameof(HomunculusAndBiomassSocket.DispenseHomunculusNow))]
    internal class HomunculusDispenseLoggerPatch
    {
        static void Postfix(HomunculusAndBiomassSocket __instance)
        {
            if (!PhotonNetwork.IsMasterClient) { return; } 

            if (__instance.Payload == null)
            {
                BepinPlugin.Log.LogWarning("HomunculusDispenseNow: Payload is null after dispense. Cannot detect Homunculus info.");
                return;
            }
            
            BepinPlugin.Log.LogInfo($"--- Homunculus Dispensed Detected ---");
            BepinPlugin.Log.LogInfo($"Homunculus GameObject Name: {__instance.Payload.gameObject.name}");
            
            if (__instance.homunculusCriteria?.orbitObjectRef != null && __instance.homunculusCriteria.compareMethod == CsObjectReference.Comparison.Prefab)
            {
                BepinPlugin.Log.LogInfo($"Homunculus Type (Asset GUID): {__instance.homunculusCriteria.orbitObjectRef.AssetGuid.ToString()}");
            }
            else if (__instance.homunculusCriteria != null && __instance.homunculusCriteria.type != null && __instance.homunculusCriteria.type.Type != null)
            {
                BepinPlugin.Log.LogInfo($"Homunculus Type (Class Name): {__instance.homunculusCriteria.type.Type.Name}");
            }
            else
            {
                BepinPlugin.Log.LogInfo("Homunculus Type (Asset GUID/Class Name): Not available or not compared by Prefab/Type.");
            }
           
            OrbitObject homunculusOrbitObject = __instance.Payload.GetComponent<OrbitObject>();
            if (homunculusOrbitObject != null)
            {
                BepinPlugin.Log.LogInfo($"Homunculus is an OrbitObject. Display Name: {homunculusOrbitObject.DisplayName}");
                BepinPlugin.Log.LogInfo($"Homunculus Faction: {homunculusOrbitObject.Faction}");

                StatTagCollection homunculusStatTagCollection = homunculusOrbitObject.Stats; 
                if (homunculusStatTagCollection != null)
                {
                    BepinPlugin.Log.LogInfo($"--- Homunculus Active Modifiers ({homunculusStatTagCollection.ActiveModifiers.Count}) ---");
                    foreach (StatMod mod in homunculusStatTagCollection.ActiveModifiers)
                    {
                        string statName = StatType.GetNameById(mod.Type, false);
                        if (string.IsNullOrEmpty(statName)) statName = $"UnknownStat({mod.Type})";

                        string modValue = "N/A";
                        if (mod.Mod is PrimitiveModifier<float> floatMod) { modValue = floatMod.Amount.ToString(); }
                        else if (mod.Mod is PrimitiveModifier<int> intMod) { modValue = intMod.Amount.ToString(); }
                        
                        string modType = mod.Mod != null ? mod.Mod.Type.ToString() ?? "NULL" : "NULL";
                        string dynamicStatus = mod.IsDynamic ? " (Dynamic)" : "";

                        BepinPlugin.Log.LogInfo($"- Stat: {statName}, Value: {modValue}, Type: {modType}{dynamicStatus}");
                    }

                    BepinPlugin.Log.LogInfo($"--- Homunculus Base Stats (Registered) ---");
                    foreach(var kvp in homunculusStatTagCollection.Stats) 
                    {
                        string statName = StatType.GetNameById(kvp.Key, false);
                        if (string.IsNullOrEmpty(statName)) statName = $"UnknownStat({kvp.Key})";
                        
                        string baseValue = "N/A";
                        if (kvp.Value.Data is ModifiableFloat floatData) { baseValue = floatData.BaseValue.ToString(); }
                        else if (kvp.Value.Data is ModifiableInt intData) { baseValue = intData.BaseValue.ToString(); }
                        else if (kvp.Value.Data is PipModifiableFloat pipFloatData) { baseValue = pipFloatData.BaseValue.ToString(); }
                        
                        BepinPlugin.Log.LogInfo($"- Stat: {statName}, Base Value: {baseValue}");
                    }
                }
                else
                {
                    BepinPlugin.Log.LogInfo("Homunculus OrbitObject does not have a StatCollection.");
                }
            }
            else
            {
                BepinPlugin.Log.LogWarning("Dispensed Homunculus is not an OrbitObject or component not found.");
            }
            BepinPlugin.Log.LogInfo($"-----------------------------------");
        }
    }
}