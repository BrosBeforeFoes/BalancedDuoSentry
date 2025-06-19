using System.Collections.Generic;

using HarmonyLib;
using CG.Game;
using CG.Objects;
using CG.Ship.Hull;
using Gameplay.Utilities;
using Gameplay.CompositeWeapons;
using CG.Ship.Modules;
using Gameplay.Carryables;

namespace BalancedDuoSentry
{

    [HarmonyPatch(typeof(ModSocket), nameof(ModSocket.OnCarryableAcquired))]
    internal class ModSocket_OnCarryableAcquired_Patch
    {
        public static GUIDUnion destroyerShipGUID = new GUIDUnion("4bc2ff9e1d156c94a9c94286a7aaa79b");
        public static DuoPlayerCondition duoPlayerCondition = new DuoPlayerCondition();

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

            // store list of new mods to add
            List<StatMod> newMods = new List<StatMod>();

            // Iterate through the list of mods *being applied by this socket*
            foreach (StatMod currentModBeingApplied in carryableMod.Modifiers)
            {
                string statName = StatType.GetNameById(currentModBeingApplied.Type, false);

                // print some logs
                string dynamicConditionType = currentModBeingApplied.DynamicCondition?.GetType().Name ?? "NULL";
                string modPrimitiveType = currentModBeingApplied.Mod?.GetType().Name ?? "NULL";
                string modType = currentModBeingApplied.Mod?.Type.ToString() ?? "NULL";
                string modAmount = "N/A";

                if (currentModBeingApplied.Mod is PrimitiveModifier<float> floatMod) modAmount = floatMod.Amount.ToString();
                else if (currentModBeingApplied.Mod is PrimitiveModifier<int> intMod) modAmount = intMod.Amount.ToString();

                BepinPlugin.Log.LogInfo($"- Processing Mod from Socket: Name={statName}, TypeID={currentModBeingApplied.Type}, Condition={dynamicConditionType}, ModPrimitive={modPrimitiveType}, ModType={modType}, Amount={modAmount}");


                // Check if this StatMod's dynamic condition is our SinglePlayerModRule
                if (currentModBeingApplied.DynamicCondition is SinglePlayerModRule)
                {

                    if (currentModBeingApplied.Type == StatType.PowerWanted.Id)
                    {
                        IntModifier currentPowerWantedMod = currentModBeingApplied.Mod as IntModifier;
                        IntModifier newIntModifier = new IntModifier(-1, currentPowerWantedMod.Type);
                        StatMod newStatMod = new StatMod(newIntModifier, StatType.PowerWanted.Id, currentModBeingApplied.TagConfiguration);
                        newStatMod.DynamicCondition = duoPlayerCondition;
                        newMods.Add(newStatMod);
                    }
                    else if (currentModBeingApplied.Type == StatType.Damage.Id)
                    {
                        FloatModifier currentDamageMod = currentModBeingApplied.Mod as FloatModifier;
                        FloatModifier newFloatModifier = new FloatModifier(0.75f, currentDamageMod.Type);
                        StatMod newStatMod = new StatMod(newFloatModifier, StatType.Damage.Id, currentModBeingApplied.TagConfiguration);
                        newStatMod.DynamicCondition = duoPlayerCondition;
                        newMods.Add(newStatMod);
                    }
                    else
                    {
                        BepinPlugin.Log.LogWarning($"- Skipping SinglePlayerModRule for unsupported StatType: {statName}");
                        continue;
                    }
                }

            }

            carryableMod.Modifiers.AddRange(newMods);

        }
    }
}
