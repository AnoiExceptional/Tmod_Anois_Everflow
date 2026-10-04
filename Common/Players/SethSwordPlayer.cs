using Terraria.ModLoader;

namespace everflow.Common.Players
{
    public sealed class SethSwordPlayer : ModPlayer
    {
        private const float BonusPerHit = 0.12f;
        private const float InternalMaximum = 0.62f;
        private const float VisibleMaximum = 0.50f;
        private const float DecayPerTick = 0.30f / 60f;

        internal float RapidAttackBonus;

        internal float AppliedDamageBonus =>
            System.Math.Min(RapidAttackBonus, VisibleMaximum);

        internal void AddRapidAttackHit()
        {
            RapidAttackBonus = System.Math.Min(
                RapidAttackBonus + BonusPerHit,
                InternalMaximum);
        }

        public override void PostUpdate()
        {
            RapidAttackBonus = System.Math.Max(
                0f,
                RapidAttackBonus - DecayPerTick);
        }
    }
}
