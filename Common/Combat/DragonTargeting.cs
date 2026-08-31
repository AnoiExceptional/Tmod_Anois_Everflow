using Microsoft.Xna.Framework;
using Terraria;

namespace everflow.Common.Combat
{
    internal static class DragonTargeting
    {
        public static void CorrectCourse(
            Projectile projectile,
            float maximumTurnDegrees,
            float maximumLeadFrames)
        {
            int targetIndex = (int)projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return;

            NPC target = Main.npc[targetIndex];
            if (!target.active || target.friendly || target.dontTakeDamage)
                return;

            float speed = projectile.velocity.Length();
            if (speed < 0.01f)
                return;

            Vector2 currentDirection = projectile.velocity / speed;
            Vector2 toTarget = target.Center - projectile.Center;

            // 目标已经越过龙头后维持当前轨迹离场，不允许整条龙急转掉头。
            if (Vector2.Dot(currentDirection, toTarget) <= 0f)
                return;

            float leadFrames = MathHelper.Clamp(
                toTarget.Length() / speed,
                0f,
                maximumLeadFrames);
            Vector2 predictedPosition = target.Center + target.velocity * leadFrames;
            Vector2 desiredDirection = (predictedPosition - projectile.Center)
                .SafeNormalize(currentDirection);

            float angleDifference = MathHelper.WrapAngle(
                desiredDirection.ToRotation() - currentDirection.ToRotation());
            float maximumTurn = MathHelper.ToRadians(maximumTurnDegrees);
            float appliedTurn = MathHelper.Clamp(angleDifference, -maximumTurn, maximumTurn);
            projectile.velocity = currentDirection.RotatedBy(appliedTurn) * speed;
        }
    }
}
