using everflow.Content.Buffs;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Players
{
    public sealed class YouFengArmorPlayer : ModPlayer
    {
        private const int DamageBoostDuration = 5 * 60;

        internal bool SetActive;
        private int damageBoostTimer;
        internal float CurrentDamageBonusPercent { get; private set; }

        public override void ResetEffects()
        {
            SetActive = false;
            CurrentDamageBonusPercent = 0f;
        }

        public override void PostUpdateEquips()
        {
            if (!SetActive)
            {
                damageBoostTimer = 0;
                Player.ClearBuff(ModContent.BuffType<YouFengBuff>());
                return;
            }

            if (damageBoostTimer <= 0)
                return;

            float remainingRatio = damageBoostTimer / (float)DamageBoostDuration;
            float damageBonus = 0.20f * remainingRatio;
            Player.GetDamage(DamageClass.Generic) += damageBonus;
            CurrentDamageBonusPercent = damageBonus * 100f;
            damageBoostTimer--;
        }

        public override void OnHurt(Player.HurtInfo info)
        {
            if (SetActive)
            {
                // 再次受击只刷新持续时间，不叠加伤害倍率。
                damageBoostTimer = DamageBoostDuration;
                Player.AddBuff(
                    ModContent.BuffType<YouFengBuff>(),
                    DamageBoostDuration);
            }
        }
    }
}
