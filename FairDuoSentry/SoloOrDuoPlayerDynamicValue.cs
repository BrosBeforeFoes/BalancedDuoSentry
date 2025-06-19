using System;
using CG.Game;
using Gameplay.Utilities;
using CG.Game.Player;

namespace FairDuoSentry
{
    [Serializable]
    public class SoloOrDuoPlayerDynamicValue : ModDynamicValue
    {
        public SoloOrDuoPlayerDynamicValue(PrimitiveModifier modifierPrimitive)
            : base()
        {
            ModifierPrimitive = modifierPrimitive;
        }

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
}
