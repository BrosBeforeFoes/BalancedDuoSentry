using Gameplay.Utilities;
using Gameplay.Tags;

namespace BalancedDuoSentry
{
    class DuoDamageStatMod : StatMod, IDescriptiveModifierSource
    {
        public DuoDamageStatMod(ModTagConfiguration tagConfig) : base(new FloatModifier(0.75f, ModifierType.AdditiveMultiplier), StatType.Damage.Id, tagConfig)
        {
        }

        public string GetDescription()
        {
            return "+75%";
        }

        public string GetHeader()
        {
            return "Blessed Homunculus";
        }
    }
}
