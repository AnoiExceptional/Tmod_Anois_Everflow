using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Common.Projectiles
{
    /// <summary>仅作用于BMPT-72火箭发射架生成并显式标记的射弹。</summary>
    public sealed class BMPT72GuidedRocket : GlobalProjectile
    {
        private const float MaximumTurnPerTick = MathHelper.Pi / 180f;

        public override bool InstancePerEntity => true;

        public bool MouseGuided { get; private set; }

        public void EnableMouseGuidance()
        {
            MouseGuided = true;
        }

        public override void PostAI(Projectile projectile)
        {
            if (!MouseGuided || projectile.owner != Main.myPlayer ||
                projectile.velocity.LengthSquared() < 0.001f)
            {
                return;
            }

            Vector2 toMouse = Main.MouseWorld - projectile.Center;
            if (toMouse.LengthSquared() < 0.001f)
                return;

            float speed = projectile.velocity.Length();
            float currentAngle = projectile.velocity.ToRotation();
            float targetAngle = toMouse.ToRotation();
            float angleDifference = MathHelper.WrapAngle(targetAngle - currentAngle);
            // extraUpdates会让PostAI在同一游戏帧执行多次。按MaxUpdates
            // 分摊角度，保证总转速仍为每帧1度（每秒60度）。
            float maximumTurnThisUpdate =
                MaximumTurnPerTick / projectile.MaxUpdates;
            float turn = MathHelper.Clamp(
                angleDifference,
                -maximumTurnThisUpdate,
                maximumTurnThisUpdate);

            if (Math.Abs(turn) < 0.0001f)
                return;

            projectile.velocity = (currentAngle + turn).ToRotationVector2() * speed;
            projectile.netUpdate = true;
        }
    }
}
