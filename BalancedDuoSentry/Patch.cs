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

    [HarmonyPatch(typeof(ModSocket), nameof(ModSocket.Start))]
    internal class ModSocket_Start_Patch
    {
        static void Postfix(ModSocket __instance)
        {
            CentralShipComputerModule centralComputer = ClientGame.Current?.PlayerShip?.GetModule<CentralShipComputerModule>();

            // Socket is not a HomunculusSocket
            if (centralComputer == null || __instance != centralComputer.HomunculusSocket) return;

            __instance.OnAcquireCarryable += OnCarriableAcquired;
        }

        private static void OnCarriableAcquired(ICarrier carrier, CarryableObject carryable, ICarrier previousCarrier)
        {
            // Payload is not a carryableMopd
            if (!(carryable is CarryableMod carryableMod)) return;

            BlessedHomunculusPatcher.ApplyPatch(carryableMod);
        }
    }
}
