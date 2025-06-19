using System;
using CG.Game;
using Gameplay.Utilities;
using CG.Game.Player;

namespace BalancedDuoSentry
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
                    floatMod.Amount = 1.00f;
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
                    intMod.Amount = -2;
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

        public override string Description()
        {
            return "In <color=#FFD700>Sentry Ship</color>";
        }

        public override string DynamicValueDescription(PrimitiveModifier mod, bool negativeValueIsGood)
        {
            String condition = ClientGame.Current.Players.Count == 1 ? "Solo" : ClientGame.Current.Players.Count == 2 ? "Duo" : "Many";

            if (ModifierPrimitive is FloatModifier floatMod)
            {
                return "<color=#00AB4D>+100%</color> if <color=#FFD700>Solo</color>, <color=#00AB4D>+75%</color> if Duo";
            }
            else if (ModifierPrimitive is IntModifier intMod)
            {
                return "<color=#00AB4D>-2</color> if <color=#FFD700>Solo</color>, <color=#00AB4D>-1</color> if Duo";
            }
            else
            {
                return "Unknown Modifier Type";
            }
        }
    }
}
