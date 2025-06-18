using HarmonyLib;
using Photon.Pun;
using System;

using CG.Game;
using CG.Objects;
using CG.Ship.Hull;
using Gameplay.Utilities;
using Gameplay.CompositeWeapons;
using CG.Ship.Modules;
using CG.Game.Player;
using Gameplay.Carryables;

namespace FairDuoSentry
{

    [Serializable]
    public class SoloOrDuoPlayerCondition : ModDynamicCondition
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
            return ClientGame.Current.Players.Count <= 2;
        }

        public override string Description()
        {
            return "#Players <= 2";
        }
    }

    [Serializable]
    public class DuoPlayerModRule : ModDynamicValue
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
            RecalculateValue();
        }

        private void OnPlayerAdded(Player obj)
        {
            RecalculateValue();
        }

        public override void RecalculateValue()
        {
            if (this.ModifierPrimitive is FloatModifier floatMod)
            {
                // Adjust float modifier value based on player count
                // Solo: +100%, Duo: +75%, Else: 0%
                if (ClientGame.Current.Players.Count == 2)
                    floatMod.Amount = 0.75f;
                else if (ClientGame.Current.Players.Count == 1)
                    floatMod.Amount = 1.25f; // TODO: adjust this value to 1.0f
                else
                    floatMod.Amount = 0.0f;
            }
            else if (this.ModifierPrimitive is IntModifier intMod)
            {
                // Adjust int modifier value based on player count
                // Solo: -2, Duo: -1, Else: 0
                if (ClientGame.Current.Players.Count == 2)
                    intMod.Amount = -1;
                else if (ClientGame.Current.Players.Count == 1)
                    intMod.Amount = -3; // TODO: adjust this value to -2
                else
                    intMod.Amount = 0;
            }
            else
            {
                BepinPlugin.Log.LogWarning($"DuoPlayerModRule: Unsupported modifier type {this.ModifierPrimitive.GetType().Name}");
            }
        }

        public override void ValidateType()
        {
            if (!(this.ModifierPrimitive is PrimitiveModifier<float> || this.ModifierPrimitive is PrimitiveModifier<int>))
            {
                throw new InvalidOperationException($"DuoPlayerModRule can only be used with FloatModifier or IntModifier, not {this.ModifierPrimitive.GetType().Name}");
            }
        }

        public override string DynamicValueDescription(PrimitiveModifier mod, bool negativeValueIsGood)
        {
            if (mod is FloatModifier floatMod)
            {
                return $"{floatMod.Amount} (Duo Player Condition)";
            }
            else if (mod is IntModifier intMod)
            {
                return $"{intMod.Amount} (Duo Player Condition)";
            }
            return "Unknown Modifier Type";
        }
    }

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
                        currentModBeingApplied.DynamicValue = new DuoPlayerModRule
                        {
                            ModifierPrimitive = currentModBeingApplied.Mod
                        };
                    }
                    // Damage modification (Id ?)
                    else if (currentModBeingApplied.Type == StatType.Damage.Id)
                    {
                        BepinPlugin.Log.LogInfo($"- Overriding DynamicCondition and DynamicValue for Damage StatMod: {statName}");
                        currentModBeingApplied.DynamicCondition = new SoloOrDuoPlayerCondition();
                        currentModBeingApplied.DynamicValue = new DuoPlayerModRule
                        {
                            ModifierPrimitive = currentModBeingApplied.Mod
                        };
                    }
                }


            }
        }
    }
}
