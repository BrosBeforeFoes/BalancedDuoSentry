using HarmonyLib;
using Photon.Pun;

using CG.Game;
using CG.Objects;
using CG.Ship.Hull;
using Gameplay.Utilities;
using Gameplay.CompositeWeapons;
using CG.Ship.Modules;
using Gameplay.Carryables;

namespace FairDuoSentry
{

    [HarmonyPatch(typeof(ModSocket), nameof(ModSocket.OnCarryableAcquired))]
    internal class ModSocket_OnCarryableAcquired_Patch
    {
        static void Prefix(ModSocket __instance, ICarrier carrier, CarryableObject carryable, ICarrier previousCarrier)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            // Identify if this ModSocket is the HomunculusSocket on the Sentry Frigate's Central Computer Module
            CentralShipComputerModule centralComputer = ClientGame.Current?.PlayerShip?.GetModule<CentralShipComputerModule>();

            if (centralComputer == null || __instance != centralComputer.HomunculusSocket)
            {
                // This is not the HomunculusSocket we care about.
                return;
            }

            if (!(carryable is CarryableMod carryableMod))
            {
                // Carryable is not a CarryableMod
                return;
            }

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
                    BepinPlugin.Log.LogInfo($"- Confirmed DynamicCondition is SinglePlayerModRule for Stat: {statName}");

                    // PowerWanted modification (Id 271581185)
                    if (currentModBeingApplied.Type == StatType.PowerWanted.Id)
                    {
                        BepinPlugin.Log.LogInfo($"- Overriding DynamicCondition and DynamicValue for PowerWanted StatMod: {statName}");
                        currentModBeingApplied.DynamicCondition = new SoloOrDuoPlayerCondition();
                        currentModBeingApplied.DynamicValue = new SoloOrDuoPlayerDynamicValue(currentModBeingApplied.Mod);
                    }
                    // Damage modification (Id ?)
                    else if (currentModBeingApplied.Type == StatType.Damage.Id)
                    {
                        BepinPlugin.Log.LogInfo($"- Overriding DynamicCondition and DynamicValue for Damage StatMod: {statName}");
                        currentModBeingApplied.DynamicCondition = new SoloOrDuoPlayerCondition();
                        currentModBeingApplied.DynamicValue = new SoloOrDuoPlayerDynamicValue(currentModBeingApplied.Mod);
                    }
                }


            }
        }
    }
}
