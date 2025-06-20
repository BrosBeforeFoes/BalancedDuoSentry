using HarmonyLib;
using CG.Game;
using CG.Ship.Hull;
using Gameplay.Utilities;
using Gameplay.CompositeWeapons;
using System;

namespace BalancedDuoSentry
{

    [HarmonyPatch(typeof(HomunculusAndBiomassSocket), nameof(HomunculusAndBiomassSocket.DispenseHomunculusNow))]
    internal class HomunculusAndBiomassSockett_DispenseHomunculusNow_Patch
    {
        private static GUIDUnion destroyerShipGUID = new GUIDUnion("4bc2ff9e1d156c94a9c94286a7aaa79b");
        private static DuoPlayerCondition duoPlayerCondition = new DuoPlayerCondition();

        static void Postfix(HomunculusAndBiomassSocket __instance)
        {
            // No payload was found
            if (__instance.Payload == null) return;

            // Carryable is not a CarryableMod
            if (!(__instance.Payload is CarryableMod carryableMod)) return;

            // This ship is not a Destroyer
            if (ClientGame.Current.playerShip.assetGuid != destroyerShipGUID) return;

            Boolean encounteredSoloPlayerCondition = false;
            Boolean encounteredDuoPlayerCondition = false;

            // check whether mods still need to be applied
            foreach (StatMod currentModBeingApplied in carryableMod.Modifiers)
            {
                if (currentModBeingApplied.DynamicCondition is SinglePlayerModRule)
                {
                    encounteredSoloPlayerCondition = true;
                }
                else if (currentModBeingApplied.DynamicCondition is DuoPlayerCondition)
                {
                    encounteredDuoPlayerCondition = true;
                }
            }

            // This is not the Lone Sentry layout
            if (!encounteredSoloPlayerCondition) return;

            // Mods have already been applied
            if (encounteredDuoPlayerCondition) return;
            
            BepinPlugin.Log.LogInfo($"Detected Destroyer with Lone Sentry layout! Applying Duo Modifications to Blessed Homunculus");

            // create new power wanted mod and add it to the homunculus
            StatMod newMod0 = new DuoPowerWantedStatMod();
            newMod0.Mod.Source = carryableMod;
            newMod0.Mod.InformationSource = carryableMod;
            newMod0.DynamicCondition = duoPlayerCondition;
            carryableMod.Modifiers.Add(newMod0);

            // create new damage mod and add it to the homunculus
            StatMod newMod1 = new DuoDamageStatMod();
            newMod1.Mod.Source = carryableMod;
            newMod1.Mod.InformationSource = carryableMod;
            newMod1.DynamicCondition = duoPlayerCondition;
            carryableMod.Modifiers.Add(newMod1);

        }
    }
}
