using System;

using CG.Game;
using Gameplay.Utilities;
using CG.Game.Player;

namespace BalancedDuoSentry
{

    [Serializable]
    public class DuoPlayerCondition : ModDynamicCondition
    {
        protected override void OnInitialize()
        {
            ClientGame.Current.ModelEventBus.OnPlayerAdded.Subscribe(OnPlayerAdded);
            ClientGame.Current.ModelEventBus.OnPlayerRemoved.Subscribe(OnPlayerRemoved);
        }

        protected override void OnDestroy()
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

        protected override bool ShouldApply()
        {
            return ClientGame.Current.Players.Count == 2;
        }

        public override string Description()
        {
            return "When <color=#FFD700>playing Duo</color>";
        }
    }

}
