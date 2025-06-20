using HarmonyLib;
using CG.Ship.Hull;
using Gameplay.CompositeWeapons;
using Gameplay.Carryables;
using CG.Objects;
using CG.Game;
using CG.Ship.Modules;

namespace BalancedDuoSentry
{
    [HarmonyPatch(typeof(HomunculusAndBiomassSocket), nameof(HomunculusAndBiomassSocket.DispenseHomunculusNow))]
    internal class HomunculusAndBiomassSocket_DispenseHomunculusNow_Patch
    {
        static void Postfix(HomunculusAndBiomassSocket __instance)
        {
            // No payload was found
            if (__instance.Payload == null) return;

            // Carryable is not a CarryableMod
            if (!(__instance.Payload is CarryableMod carryableMod)) return;

            BlessedHomunculusPatcher.ApplyPatch(carryableMod);
        }
    }

    [HarmonyPatch(typeof(ModSocket), nameof(ModSocket.OnCarryableAcquired))]
    internal class ModSocket_OnCarryableAcquired_Patch
    {
        static void Prefix(ModSocket __instance, ICarrier carrier, CarryableObject carryable, ICarrier previousCarrier)
        {
            CentralShipComputerModule centralComputer = ClientGame.Current?.PlayerShip?.GetModule<CentralShipComputerModule>();

            // Socket is not a HomunculusSocket
            if (centralComputer == null || __instance != centralComputer.HomunculusSocket) return;

            // Payload is not a carryableMopd
            if (!(carryable is CarryableMod carryableMod)) return;

            BlessedHomunculusPatcher.ApplyPatch(carryableMod);
        }
    }
}
