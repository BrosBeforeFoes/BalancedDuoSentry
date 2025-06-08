using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Photon.Pun;
using Gameplay.Utilities;
using CG.Space;
using CG.Ship.Hull;
using ToolClasses;
using System.Collections.Generic;
using CG.Objects;
using CG.Game;
using CG.Client.Utils;
using ResourceAssets;
using Gameplay.Mutators;


namespace FairDuoSentry
{
    [HarmonyPatch(typeof(CG.Ship.Hull.HomunculusAndBiomassSocket), nameof(CG.Ship.Hull.HomunculusAndBiomassSocket.DispenseHomunculusNow))]
    internal class HomunculusDispensePatch
    {
        static void Postfix(HomunculusAndBiomassSocket __instance)
        {
            // Only log if we are the MasterClient (host) to avoid duplicate logs in multiplayer
            // if (!PhotonNetwork.IsMasterClient) { return; } 

            if (__instance.Payload == null)
            {
                BepinPlugin.Log.LogWarning("HomunculusDispenseNow: Payload is null after dispense. Cannot detect Homunculus info.");
                return;
            }

            // --- Log current ship's asset GUID and player count ---
            // Accessing PlayerShip from ClientGame.Current.PlayerShip (capital P)
            // Using .ContainerGuid for the ship type/loadout GUID
            if (ClientGame.Current != null && ClientGame.Current.PlayerShip != null)
            {
                BepinPlugin.Log.LogInfo($"Current Ship Name: {ClientGame.Current.PlayerShip.gameObject.name}");
                BepinPlugin.Log.LogInfo($"Current Ship Type (Loadout Container GUID): {ClientGame.Current.PlayerShip.ContainerGuid}");
            }
            else
            {
                BepinPlugin.Log.LogInfo("ClientGame.Current.PlayerShip is not available.");
            }

            // Player count using RoomPlayersTracker
            if (RoomPlayersTracker.Instance != null && RoomPlayersTracker.Instance.Players != null)
            {
                BepinPlugin.Log.LogInfo($"Current Player Count: {RoomPlayersTracker.Instance.Players.Count}");
            }
            else
            {
                BepinPlugin.Log.LogInfo("RoomPlayersTracker.Instance or its Players dictionary is not available.");
            }

            // --- Homunculus Detection ---
            CarryableObject homunculusCarryable = __instance.Payload;
            BepinPlugin.Log.LogInfo($"--- Homunculus Dispensed Detected ---");
            BepinPlugin.Log.LogInfo($"Homunculus GameObject Name: {homunculusCarryable.gameObject.name}");

            // Access AssetGuid correctly via orbitObjectRef
            if (__instance.homunculusCriteria != null && __instance.homunculusCriteria.compareMethod == CsObjectReference.Comparison.Prefab && __instance.homunculusCriteria.orbitObjectRef != null)
            {
                // GUIDUnion is a struct, so .ToString() is typically used to get its string representation
                BepinPlugin.Log.LogInfo($"Homunculus Type (Asset GUID): {__instance.homunculusCriteria.orbitObjectRef.AssetGuid.ToString()}");
            }
            else if (__instance.homunculusCriteria != null && __instance.homunculusCriteria.type != null && __instance.homunculusCriteria.type.Type != null)
            {
                // Log by type if not by prefab
                BepinPlugin.Log.LogInfo($"Homunculus Type (Class Name): {__instance.homunculusCriteria.type.Type.Name}");
            }
            else
            {
                BepinPlugin.Log.LogInfo("Homunculus Type (Asset GUID/Class Name): Not available or not compared by Prefab/Type.");
            }

            // Attempt to get the OrbitObject component on the Homunculus
            OrbitObject homunculusOrbitObject = homunculusCarryable.GetComponent<OrbitObject>();
            if (homunculusOrbitObject != null)
            {
                BepinPlugin.Log.LogInfo($"Homunculus is an OrbitObject. Display Name: {homunculusOrbitObject.DisplayName}");
                BepinPlugin.Log.LogInfo($"Homunculus Faction: {homunculusOrbitObject.Faction}");

                // Access and log its StatMods
                // homunculusOrbitObject.Stats is already a StatTagCollection (which inherits StatCollection)
                StatTagCollection homunculusStatTagCollection = homunculusOrbitObject.Stats;
                if (homunculusStatTagCollection != null)
                {
                    // ActiveModifiers is directly on StatCollection (and thus StatTagCollection)
                    BepinPlugin.Log.LogInfo($"--- Homunculus Active Modifiers ({homunculusStatTagCollection.ActiveModifiers.Count}) ---");
                    if (homunculusStatTagCollection.ActiveModifiers.Count == 0)
                    {
                        BepinPlugin.Log.LogInfo("No active StatMods found directly on Homunculus's StatCollection.");
                    }
                    else
                    {
                        foreach (StatMod mod in homunculusStatTagCollection.ActiveModifiers)
                        {
                            string statName = StatType.GetNameById(mod.Type, false);
                            if (string.IsNullOrEmpty(statName)) statName = $"UnknownStat({mod.Type})";

                            // Correctly access the Amount from PrimitiveModifier<T>
                            string modValue = "N/A";
                            if (mod.Mod is PrimitiveModifier<float> floatMod)
                            {
                                modValue = floatMod.Amount.ToString();
                            }
                            else if (mod.Mod is PrimitiveModifier<int> intMod)
                            {
                                modValue = intMod.Amount.ToString();
                            }

                            string modType = mod.Mod != null ? mod.Mod.Type.ToString() : "N/A";
                            string dynamicStatus = mod.IsDynamic ? " (Dynamic)" : "";

                            BepinPlugin.Log.LogInfo($"- Stat: {statName}, Value: {modValue}, Type: {modType}{dynamicStatus}");
                        }
                    }

                    // Log base stats from the Dictionary<int, StatBase> Stats property on StatCollection
                    BepinPlugin.Log.LogInfo($"--- Homunculus Base Stats (Registered) ---");
                    foreach (var kvp in homunculusStatTagCollection.Stats) // This refers to the Dictionary<int, StatBase> Stats in StatCollection
                    {
                        string statName = StatType.GetNameById(kvp.Key, false);
                        if (string.IsNullOrEmpty(statName)) statName = $"UnknownStat({kvp.Key})";

                        string baseValue = "N/A";
                        if (kvp.Value.Data is ModifiableFloat floatData)
                            baseValue = floatData.BaseValue.ToString();
                        else if (kvp.Value.Data is ModifiableInt intData)
                            baseValue = intData.BaseValue.ToString();

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

    [HarmonyPatch(typeof(SinglePlayerModRule), nameof(SinglePlayerModRule.ShouldApply))]
    internal class SinglePlayerModRulePatch
    {
        // Use a Prefix to override the original ShouldApply method
        static bool Prefix(ref bool __result)
        {
            int playerCount = RoomPlayersTracker.Instance.Players.Count;
            // Set __result to true if 1 or 2 players, false otherwise.
            __result = (playerCount == 1 || playerCount == 2);
            BepinPlugin.Log.LogInfo($"!!!! SinglePlayerModRule.ShouldApply patched. Player count: {playerCount}, Returning: {__result}");

            // Return false to skip the original method and use our __result
            return false;
        }
    }
    
    [HarmonyPatch(typeof(MutatorEffectOrbitObjectStatMod), nameof(MutatorEffectOrbitObjectStatMod.OnMutatorActivated))]
    internal class MutatorEffectOrbitObjectStatModPatch
    {
        static void Postfix(MutatorEffectOrbitObjectStatMod __instance)
        {
            // Only apply on the MasterClient (host) to prevent desyncs
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            // Identify the "Blessed Homunculus" mutator effect.
            // This is crucial. You need a reliable way to identify this specific mutator.
            // Options:
            // 1. Check if the mutator's name or AssetGuid matches (requires obtaining the GUID of the "Blessed Homunculus" Mutator).
            // 2. Check if its 'Mods' array contains StatMods for PowerWanted and Damage, which is characteristic of this mutator.

            // For now, let's assume this patch only applies to the Blessed Homunculus mutator due to its context.
            // If there are other MutatorEffectOrbitObjectStatMod instances you don't want to affect,
            // you'll need to add an if-condition here to check `__instance.Mutator.ContainerGuid`
            // against the actual GUID of the Blessed Homunculus Mutator.

            int playerCount = RoomPlayersTracker.Instance.Players.Count;
            BepinPlugin.Log.LogInfo($"!!!MutatorEffectOrbitObjectStatMod.OnMutatorActivated Postfix triggered. Player count: {playerCount}");

            foreach (StatMod mod in __instance.Mods)
            {
                
                // Identify the PowerWanted StatMod
                if (mod.Type == StatType.PowerWanted.Id)
                {
                    if (mod.Mod is PrimitiveModifier<float> floatMod) // PowerWanted is typically float
                    {
                        float newPowerWantedValue = 0f;
                        if (playerCount == 1)
                        {
                            newPowerWantedValue = -2f; // Solo: Power Consumption: -2
                        }
                        else if (playerCount == 2)
                        {
                            newPowerWantedValue = -1f; // Duo: Power Consumption: -1
                        }
                        // For 3+ players, leave as 0 or disable the mod.
                        // Since ShouldApply is now true for 1 or 2 players, for 3+ it won't be active via SinglePlayerModRule,
                        // so we don't need to explicitly set to 0 here for that case.
                        // The base value for 3+ players should be its default if the mod deactivates.
                        // However, since we forced SinglePlayerModRule.ShouldApply() to be true for 1 and 2 players,
                        // this mod will *always* be active for 1 and 2 players, and inactive for 3+.

                        // Only apply if there's a specific modifier for 1 or 2 players.
                        if (playerCount == 1 || playerCount == 2)
                        {
                            floatMod.Amount = newPowerWantedValue;
                            // Optionally, ensure the condition is cleared or set to always active if not already
                            // The SinglePlayerModRule patch ensures the *condition* allows it.
                            // Here, we just adjust the value.
                            mod.DynamicCondition = null; // Ensure dynamic condition doesn't interfere with this value
                            BepinPlugin.Log.LogInfo($"!!!!!!!! Modified PowerWanted to: {floatMod.Amount} for {playerCount} players.");
                        }
                    }
                }
                // Identify the Damage StatMod
                else if (mod.Type == StatType.Damage.Id)
                {
                    if (mod.Mod is PrimitiveModifier<float> floatMod) // Damage is typically float (or int, confirm with logs)
                    {
                        float newDamageValue = 0f; // 100% means 1.0 (multiplicative), 75% means 0.75
                                                   // Note: Damage buffs are often multiplicative modifiers, where 1.0 is +100%.
                                                   // Confirm if the original +100% means ModifierType.Multiplicative with Value 1.0,
                                                   // or ModifierType.Additive with Value 100 (less likely for percentage).
                                                   // Assuming it's a multiplicative modifier where 1.0 means +100%.

                        if (playerCount == 1)
                        {
                            newDamageValue = 1.0f; // Solo: Damage: +100% (or 1.0 for multiplicative)
                        }
                        else if (playerCount == 2)
                        {
                            newDamageValue = 0.75f; // Duo: Damage: +75% (or 0.75 for multiplicative)
                        }

                        if (playerCount == 1 || playerCount == 2)
                        {
                            floatMod.Amount = newDamageValue;
                            mod.DynamicCondition = null; // Ensure dynamic condition doesn't interfere
                            BepinPlugin.Log.LogInfo($"!!!!!!!! Modified Damage to: {floatMod.Amount} (multiplier) for {playerCount} players.");
                        }
                    }
                }
            }
            // Trigger a refresh of the StatCollection if necessary, as the values have changed.
            // The GameSessionManager.ActiveSession.RegisterGlobalOrbitObjectMods might already do this,
            // but explicitly refreshing if needed can be done by invoking OnModifiersChange on relevant StatCollections.
            // For now, rely on the game's internal refresh logic after `RegisterGlobalOrbitObjectMods`
            // is called by the original method (since this is a Postfix).
        }
    }
}