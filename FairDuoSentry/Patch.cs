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
using CG.Ship.Modules;
using CG.Game.Player;
using Gameplay.Carryables;


namespace FairDuoSentry
{

    // --- NEW MOD RULE: DuoPlayerModRule ---
    [Serializable]
	public class DuoPlayerModRule : ModDynamicCondition
	{
		public override void OnInitialize()
		{
			ClientGame.Current.ModelEventBus.OnPlayerAdded.Subscribe(OnPlayerAdded);
			ClientGame.Current.ModelEventBus.OnPlayerRemoved.Subscribe(OnPlayerRemoved);
		}

		public override void OnDestroy()
		{
			ClientGame.Current.ModelEventBus.OnPlayerAdded.Unsubscribe(OnPlayerAdded);
			ClientGame.Current.ModelEventBus.OnPlayerRemoved.Unsubscribe(OnPlayerRemoved);
		}

		private void OnPlayerRemoved(Player obj)
		{
			CheckIfActive();
		}

		private void OnPlayerAdded(Player obj)
		{
			CheckIfActive();
		}

		public override bool ShouldApply()
		{
			return ClientGame.Current.Players.Count == 2;
		}
	}

    // --- NEW TARGET PATCH: Dynamically Adjust StatMod values via ModSocket.ApplyMods ---
    [HarmonyPatch(typeof(ModSocket), nameof(ModSocket.OnCarryableAcquired))]
    internal class ModSocket_OnCarryableAcquired_Patch
    {
        // Use Postfix to run after original mods are applied by the socket
        static void Prefix(ModSocket __instance, ICarrier carrier, CarryableObject carryable, ICarrier previousCarrier)
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

            if (!(carryable is CarryableMod carryableMod))
            {
                BepinPlugin.Log.LogWarning($"ModSocket_OnCarryableAcquired: Carryable is not a CarryableMod. Cannot apply mods.");
                return;
            }

            List<StatMod> newMods = new List<StatMod>();


            BepinPlugin.Log.LogInfo($"[DEBUG - ModSocket.ApplyMods] Checking {carryableMod.Modifiers.Count} mods in list...");

            // Iterate through the list of mods *being applied by this socket*
            foreach (StatMod currentModBeingApplied in carryableMod.Modifiers)
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
                        if (currentModBeingApplied.Mod is IntModifier powerIntMod)
                        {
                            // public StatMod(PrimitiveModifier mod, int type, ModTagConfiguration tagConfig)
                            StatMod newMod = new StatMod(
                                new IntModifier(-1, powerIntMod.Type),
                                StatType.PowerWanted.Id,
                                currentModBeingApplied.TagConfiguration
                            );
                            newMod.DynamicCondition = new DuoPlayerModRule(); // Set our new dynamic condition
                            newMod.SetSource(currentModBeingApplied.Mod.Source); // Copy the source from the original mod
                            newMod.InitDynamicElements(); // Initialize the dynamic condition
                            newMods.Add(newMod); // Add to the list of new mods to apply

                        }
                        else
                        {
                            BepinPlugin.Log.LogWarning($"- PowerWanted mod from socket.Mod is NOT PrimitiveModifier<int>. Actual type: {modPrimitiveType}");
                        }
                    }
                    // Damage modification (Id 1)
                    else if (currentModBeingApplied.Type == StatType.Damage.Id)
                    {
                        if (currentModBeingApplied.Mod is FloatModifier damageFloatMod)
                        {
                            // public StatMod(PrimitiveModifier mod, int type, ModTagConfiguration tagConfig)
                            StatMod newMod = new StatMod(
                                new FloatModifier(0.75f, damageFloatMod.Type),
                                StatType.Damage.Id,
                                currentModBeingApplied.TagConfiguration
                            );
                            newMod.DynamicCondition = new DuoPlayerModRule(); // Set our new dynamic condition
                            newMod.SetSource(currentModBeingApplied.Mod.Source); // Copy the source from the original mod
                            newMod.InitDynamicElements(); // Initialize the dynamic condition
                            newMods.Add(newMod); // Add to the list of new mods to apply


                            StatMod newMod2 = new StatMod(
                                new FloatModifier(0.75f, damageFloatMod.Type),
                                StatType.FireRate.Id,
                                currentModBeingApplied.TagConfiguration
                            );
                            newMod2.DynamicCondition = new SinglePlayerModRule();
                            newMod2.SetSource(currentModBeingApplied.Mod.Source); // Copy the source from the original mod
                            newMod2.InitDynamicElements(); // Initialize the dynamic condition
                            newMods.Add(newMod2); // Add to the list of new mods to apply

                        }
                        else
                        {
                            BepinPlugin.Log.LogWarning($"- Damage mod from socket.Mod is NOT PrimitiveModifier<float>. Actual type: {modPrimitiveType}");
                        }
                    }
                }


            }
            
            carryableMod.Modifiers.AddRange(newMods); // Add our new mods to the carryable's modifiers
            BepinPlugin.Log.LogInfo($"[DEBUG - ModSocket.ApplyMods] Added {newMods.Count} new mods to CarryableMod '{carryableMod.DisplayName}'.");

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


            OrbitObject homunculusOrbitObject = __instance.Payload.GetComponent<OrbitObject>();
            if (homunculusOrbitObject != null)
            {
                BepinPlugin.Log.LogInfo($"Homunculus is an OrbitObject. Display Name: {homunculusOrbitObject.DisplayName}");
                BepinPlugin.Log.LogInfo($"Homunculus Faction: {homunculusOrbitObject.Faction}");

            }
            else
            {
                BepinPlugin.Log.LogWarning("Dispensed Homunculus is not an OrbitObject or component not found.");
            }
            BepinPlugin.Log.LogInfo($"-----------------------------------");
        }
    }
}