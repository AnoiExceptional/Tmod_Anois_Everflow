using everflow.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>仅为悲鸣冰川实际发射的弹药追加命中效果。</summary>
    public sealed class GlacialLamentProjectile : GlobalProjectile
    {
        private const int FiveSeconds = 5 * 60;
        private const int ThreeSeconds = 3 * 60;

        public override bool InstancePerEntity => true;

        private bool enabled;
        private bool freezingShot;

        public void Enable(bool isFreezingShot)
        {
            enabled = true;
            freezingShot = isFreezingShot;
        }

        public override void OnHitNPC(
            Projectile projectile,
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            if (!enabled)
                return;

            // 寒霜套使用的是强化霜火版本；普通Frostburn每跳伤害更低。
            target.AddBuff(BuffID.Frostburn2, FiveSeconds);

            MonsterColdControlNPC.TryApplyChilled(target, FiveSeconds);

            if (freezingShot)
                MonsterColdControlNPC.TryApplyFrozen(target, ThreeSeconds);

            // 即使所用弹药原本具有穿透能力，也只允许命中一个目标。
            projectile.Kill();
        }
    }
}
