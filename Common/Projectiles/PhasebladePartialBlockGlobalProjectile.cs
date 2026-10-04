using Terraria;
using Terraria.ModLoader;

namespace everflow.Common.Projectiles
{
    /// <summary>
    /// 魔力不足时，射弹不会被删除，而是继续完成一次正常命中。
    /// 这里只缩放该玩家承受的最终伤害，因此原射弹及其他GlobalProjectile
    /// 的OnHitPlayer仍会照常执行，完整保留命中减益与特殊效果。
    /// </summary>
    public sealed class PhasebladePartialBlockGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        internal int TargetPlayer = -1;
        internal float DamageMultiplier = 1f;
        internal int TimeLeft;

        internal bool IsPendingFor(Player player) =>
            TimeLeft > 0 && TargetPlayer == player.whoAmI;

        internal void SetPartialBlock(Player player, float unpaidRatio)
        {
            TargetPlayer = player.whoAmI;
            DamageMultiplier = System.Math.Clamp(unpaidRatio, 0f, 1f);
            TimeLeft = 60;
        }

        public override void PostAI(Projectile projectile)
        {
            if (TimeLeft > 0 && --TimeLeft == 0)
                ResetPartialBlock();
        }

        public override void ModifyHitPlayer(
            Projectile projectile,
            Player target,
            ref Player.HurtModifiers modifiers)
        {
            if (IsPendingFor(target))
                modifiers.FinalDamage *= DamageMultiplier;
        }

        public override void OnHitPlayer(
            Projectile projectile,
            Player target,
            Player.HurtInfo info)
        {
            if (TargetPlayer == target.whoAmI)
                ResetPartialBlock();
        }

        private void ResetPartialBlock()
        {
            TargetPlayer = -1;
            DamageMultiplier = 1f;
            TimeLeft = 0;
        }
    }
}
