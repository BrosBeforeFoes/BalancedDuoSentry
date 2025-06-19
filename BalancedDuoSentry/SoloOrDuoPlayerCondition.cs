using System;

using CG.Game;
using Gameplay.Utilities;
using CG.Game.Player;

namespace BalancedDuoSentry
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

}
