using HarmonyLib;
using CG.Game;
using CG.Objects;
using CG.Ship.Hull;
using Gameplay.Utilities;
using Gameplay.CompositeWeapons;
using CG.Ship.Modules;
using Gameplay.Carryables;
using System;

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
            StatMod damageStatMod = null;
            StatMod powerWantedStatMod = null;

            // Iterate through the list of mods *being applied by this socket*
            foreach (StatMod currentModBeingApplied in carryableMod.Modifiers)
            {
                if (currentModBeingApplied.DynamicCondition is SinglePlayerModRule)
                {
                    encounteredSoloPlayerCondition = true;

                    if (currentModBeingApplied.Type == StatType.Damage.Id)
                    {
                        // Store the damage stat mod for later
                        damageStatMod = currentModBeingApplied;
                    }
                    else if (currentModBeingApplied.Type == StatType.PowerWanted.Id)
                    {
                        // Store the power wanted stat mod for later
                        powerWantedStatMod = currentModBeingApplied;
                    }
                }
                else if (currentModBeingApplied.DynamicCondition is DuoPlayerCondition)
                {
                    encounteredDuoPlayerCondition = true;
                }
            }

            if (encounteredSoloPlayerCondition && !encounteredDuoPlayerCondition)
            {
                // create new power wanted mod and add it to the homunculus
                StatMod newStatMod = new DuoPowerWantedStatMod(powerWantedStatMod.TagConfiguration);
                newStatMod.DynamicCondition = duoPlayerCondition;
                carryableMod.Modifiers.Add(newStatMod);

                // create new damage mod and add it to the homunculus
                StatMod newDamageStatMod = new DuoDamageStatMod(damageStatMod.TagConfiguration);
                newDamageStatMod.DynamicCondition = duoPlayerCondition;
                carryableMod.Modifiers.Add(newDamageStatMod);
            }

        }
    }
}
