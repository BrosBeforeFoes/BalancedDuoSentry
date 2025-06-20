using CG.Game;
using Gameplay.Utilities;
using Gameplay.CompositeWeapons;

namespace BalancedDuoSentry
{
    class BlessedHomunculusPatcher
    {
        private static GUIDUnion destroyerShipGUID = new GUIDUnion("4bc2ff9e1d156c94a9c94286a7aaa79b");
        private static DuoPlayerCondition duoPlayerCondition = new DuoPlayerCondition();

        public static void ApplyPatch(CarryableMod homunculus)
        {
            // This ship is not a Destroyer
            if (ClientGame.Current.playerShip.assetGuid != destroyerShipGUID) return;

            StatMod damageStatMod = null;
            StatMod powerWantedStatMod = null;

            // check whether mods still need to be applied
            foreach (StatMod currentModBeingApplied in homunculus.Modifiers)
            {
                // Stop if custom modifiers have already been applied
                if (currentModBeingApplied.DynamicCondition is DuoPlayerCondition) return;

                if (currentModBeingApplied.DynamicCondition is SinglePlayerModRule)
                {
                    if (currentModBeingApplied.Type == StatType.Damage.Id)
                    {
                        damageStatMod = currentModBeingApplied;
                    }
                    else if (currentModBeingApplied.Type == StatType.PowerWanted.Id)
                    {
                        powerWantedStatMod = currentModBeingApplied;
                    }
                }
            }

            // create new power wanted mod and add it to the homunculus
            if (powerWantedStatMod != null) {
                BepinPlugin.Log.LogInfo($"Applying Duo Power Wanted Mod to Blessed Homunculus");
                StatMod newMod0 = new DuoPowerWantedStatMod(powerWantedStatMod.TagConfiguration);
                newMod0.Mod.Source = homunculus;
                newMod0.Mod.InformationSource = homunculus;
                newMod0.DynamicCondition = duoPlayerCondition;
                homunculus.Modifiers.Add(newMod0);
            }

            // create new damage mod and add it to the homunculus
            if (damageStatMod != null) {
                BepinPlugin.Log.LogInfo($"Applying Duo Damage Mod to Blessed Homunculus");
                StatMod newMod1 = new DuoDamageStatMod(damageStatMod.TagConfiguration);
                newMod1.Mod.Source = homunculus;
                newMod1.Mod.InformationSource = homunculus;
                newMod1.DynamicCondition = duoPlayerCondition;
                homunculus.Modifiers.Add(newMod1);
            }

        }
    }
}
