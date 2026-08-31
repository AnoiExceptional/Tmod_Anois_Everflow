using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Players
{
    public sealed class LegionAuthorityPlayer : ModPlayer
    {
        public float NonSummonDamageMultiplier = 1f;

        public override void ResetEffects()
        {
            NonSummonDamageMultiplier = 1f;
        }

        public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsPenalizedClass(item.DamageType))
                modifiers.FinalDamage *= NonSummonDamageMultiplier;
        }

        public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (IsPenalizedClass(proj.DamageType))
                modifiers.FinalDamage *= NonSummonDamageMultiplier;
        }

        private static bool IsPenalizedClass(DamageClass damageClass)
        {
            // 召唤近战速度类鞭子同时继承近战属性，必须先明确排除召唤伤害。
            if (damageClass.CountsAsClass(DamageClass.Summon))
                return false;

            return damageClass.CountsAsClass(DamageClass.Melee)
                || damageClass.CountsAsClass(DamageClass.Ranged)
                || damageClass.CountsAsClass(DamageClass.Magic);
        }
    }
}
