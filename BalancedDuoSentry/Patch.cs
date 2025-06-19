using HarmonyLib;
using CG.Game;
using CG.Objects;
using CG.Ship.Hull;
using Gameplay.Utilities;
using Gameplay.CompositeWeapons;
using CG.Ship.Modules;
using Gameplay.Carryables;
using System;
using Gameplay.Tags;

namespace BalancedDuoSentry
{

    [HarmonyPatch(typeof(ModSocket), nameof(ModSocket.OnCarryableAcquired))]
    internal class ModSocket_OnCarryableAcquired_Patch
    {
        private static GUIDUnion destroyerShipGUID = new GUIDUnion("4bc2ff9e1d156c94a9c94286a7aaa79b");
        private static DuoPlayerCondition duoPlayerCondition = new DuoPlayerCondition();

        static void Prefix(ModSocket __instance, ICarrier carrier, CarryableObject carryable, ICarrier previousCarrier)
        {
            if (ClientGame.Current.playerShip.assetGuid != destroyerShipGUID)
            {
                // This ship is not a Destroyer
                return;
            }

            // Identify if this ModSocket is the HomunculusSocket on the Sentry Frigate's Central Computer Module
            CentralShipComputerModule centralComputer = ClientGame.Current?.PlayerShip?.GetModule<CentralShipComputerModule>();

            if (centralComputer == null || __instance != centralComputer.HomunculusSocket)
            {
                // This socket is not the HomunculusSocket
                return;
            }

            if (!(carryable is CarryableMod carryableMod))
            {
                // Carryable is not a CarryableMod
                return;
            }

            Boolean encounteredSoloPlayerCondition = false;
            Boolean encounteredDuoPlayerCondition = false;

            // Iterate through the list of mods *being applied by this socket*
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

            if (encounteredSoloPlayerCondition && !encounteredDuoPlayerCondition)
            {
                // carryableMod.Modifiers.Clear(); // Clear all mods being applied by this socket

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
}
