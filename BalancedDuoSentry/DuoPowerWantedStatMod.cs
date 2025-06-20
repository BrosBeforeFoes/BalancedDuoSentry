using Gameplay.Utilities;
using Gameplay.Tags;

namespace BalancedDuoSentry
{

    class DuoPowerWantedStatMod : StatMod, IDescriptiveModifierSource
    {

        public DuoPowerWantedStatMod(ModTagConfiguration tagConfig) : base(new IntModifier(-1, ModifierType.PrimaryAddend), StatType.PowerWanted.Id, tagConfig)
        {
        }

        public string GetDescription()
        {
            return "-1";
        }

        public string GetHeader()
        {
            return "Blessed Homunculus";
        }
    }

}
