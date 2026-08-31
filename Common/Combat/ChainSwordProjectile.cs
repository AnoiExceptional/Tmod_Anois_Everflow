using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Common.Combat
{
    public readonly record struct ChainSwordSettings(
        int NodeCount = 15,
        bool DrawBodyCurve = false,
        float NodeScale = 1f,
        float HandleScale = 1f,
        float SwingAngle = 3.6f,
        float TipLagAngle = 1.65f,
        float MaxAttackDistance = 240f,
        bool AlternateReverse = false,
        bool BodyCurveGlows = false,
        bool NodesGlow = false,
        float CollapsedTipLagAngle = 2.8274334f);

    public struct ChainSwordSwingState
    {
        private int swingCount;
        public readonly bool PreviewNext(bool enabled) => enabled && (swingCount + 1) % 2 == 0;
        public bool Advance(bool enabled) { swingCount++; return enabled && swingCount % 2 == 0; }
    }

    /// <summary>可复用的链剑挥舞类型；派生类只需声明八项配置和贴图切帧。</summary>
    public abstract class ChainSwordProjectile : ModProjectile
    {
        protected abstract ChainSwordSettings Settings { get; }
        protected virtual float HandlePivotExtension => 8f;
        protected virtual Color BodyCurveColor => Color.White;
        protected virtual float BodyCurveWidth => 2f;
        protected virtual float NodeGlowStrength => 0.35f;
        protected virtual Color NodeGlowColor => Color.White;
        protected abstract Rectangle GetHandleFrame(Texture2D texture);
        protected abstract Rectangle GetNodeFrame(Texture2D texture, int nodeIndex);
        protected virtual void ConfigureChainSwordProjectile() { }
        protected virtual void UpdateChainSword(IReadOnlyList<Vector2> points) { }

        public sealed override void SetStaticDefaults() => ProjectileID.Sets.IsAWhip[Type] = true;

        public sealed override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.SwordWhip);
            Projectile.penetrate = Projectile.maxPenetrate = -1;
            Projectile.WhipSettings.Segments = Math.Max(1, Settings.NodeCount);
            Projectile.WhipSettings.RangeMultiplier = 1f;
            ConfigureChainSwordProjectile();
        }

        public sealed override bool PreAI()
        {
            if (Projectile.ai[0] == 0f) Projectile.ai[0] = 0.001f;
            return true;
        }

        public sealed override void AI() => UpdateChainSword(GetControlPoints());

        public sealed override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Rectangle hitbox = new(0, 0, projHitbox.Width, projHitbox.Height);
            foreach (Vector2 point in GetControlPoints())
            {
                hitbox.X = (int)point.X - hitbox.Width / 2;
                hitbox.Y = (int)point.Y - hitbox.Height / 2;
                if (hitbox.Intersects(targetHitbox)) return true;
            }
            return false;
        }

        public sealed override bool PreDraw(ref Color lightColor)
        {
            IReadOnlyList<Vector2> points = GetControlPoints();
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            SpriteEffects effects = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            if (Settings.DrawBodyCurve) DrawCurve(points);
            DrawNodes(texture, points, effects);
            return false;
        }

        private List<Vector2> GetControlPoints()
        {
            List<Vector2> points = new();
            Player owner = Main.player[Projectile.owner];
            float duration = owner.itemAnimationMax * Projectile.MaxUpdates;
            if (duration <= 0f) return points;

            float progress = MathHelper.Clamp(Projectile.ai[0] / duration, 0f, 1f);
            float extension = progress * 1.5f;
            if (extension > 1f) extension = MathHelper.Lerp(1f, 0f, (extension - 1f) / 0.5f);
            extension = MathHelper.Clamp(extension, 0f, 1f);
            int segments = Math.Max(1, Settings.NodeCount);
            float segmentLength = Settings.MaxAttackDistance * owner.whipRangeMultiplier * extension / segments;

            float sign = Projectile.spriteDirection;
            if (Settings.AlternateReverse && Projectile.ai[2] == 1f) sign *= -1f;
            // 匀减速角运动：easeOutQuad 的角速度从起手最大值线性下降，
            // 到挥舞结束时趋近于零。总角度与动画总时长均保持不变。
            float remaining = 1f - progress;
            float eased = 1f - remaining * remaining;
            float halfSwing = Settings.SwingAngle * 0.5f;
            // 匀减速后，链刃达到最大伸展时的轨迹会落在鼠标方向下方约30°。
            // 按角色朝向反向补偿瞄准基准，使左右两侧都在视觉上向上校准，
            // 从而让最大攻击距离所在方向重新对准鼠标。
            // 补偿必须跟随本次实际挥舞方向，而不只是角色朝向。启用
            // AlternateReverse 的偶数次攻击会翻转 sign，瞄准补偿也随之
            // 翻转，避免视觉轨迹相对鼠标累计出60°误差。
            float aimCorrection = -sign * MathHelper.ToRadians(30f);
            float leadingAngle = Projectile.velocity.ToRotation() + aimCorrection +
                sign * MathHelper.Lerp(-halfSwing, halfSwing, eased);
            Rectangle handle = GetHandleFrame(TextureAssets.Projectile[Type].Value);
            float pivotOffset = handle.Height * Settings.HandleScale * 0.5f + HandlePivotExtension;
            Vector2 current = Main.GetPlayerArmPosition(Projectile) + leadingAngle.ToRotationVector2() * pivotOffset;
            points.Add(current);

            // 刚出手和即将完全收回时，末端接近反向折回；随着链身伸展，
            // 曲率平滑降低到武器自身的常规尾端滞后角。因为 extension 在
            // 伸展/收回两端均为0，所以两端自然获得原版鞭子般的大弧度。
            float maxLag = MathHelper.Lerp(
                Settings.CollapsedTipLagAngle,
                Settings.TipLagAngle,
                extension);
            // 收拢时使用更明显的螺旋曲率；完全伸展时恢复旧版1.25的
            // 分布，避免剑尖在最大攻击距离处仍然弯折过度。
            float curvatureExponent = MathHelper.Lerp(1.65f, 1.25f, extension);
            for (int i = 1; i <= segments; i++)
            {
                float nodeProgress = i / (float)segments;
                // 较高的幂次让剑柄附近保持较直，曲率沿链身逐渐增强，
                // 到剑尖形成近乎朝后的螺旋式弯折。
                float lag = maxLag * MathF.Pow(nodeProgress, curvatureExponent);
                current += (leadingAngle - sign * lag).ToRotationVector2() * segmentLength;
                points.Add(current);
            }
            return points;
        }

        private void DrawCurve(IReadOnlyList<Vector2> points)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle source = new(0, 0, 1, 1);
            // 最后一个控制点只负责定义末节朝向与碰撞轨迹，并没有对应的
            // 节点贴图。曲线必须和节点绘制一样停在倒数第二个控制点，
            // 否则就会从剑尖中心继续伸出一个完整节距的线头。
            for (int i = 0; i < points.Count - 2; i++)
            {
                Vector2 delta = points[i + 1] - points[i];
                Vector2 center = (points[i] + points[i + 1]) * 0.5f;
                Color curveColor = Settings.BodyCurveGlows
                    ? BodyCurveColor
                    : MultiplyByLighting(BodyCurveColor, center);
                Main.EntitySpriteDraw(pixel, center - Main.screenPosition, source, curveColor,
                    delta.ToRotation(), new Vector2(0.5f, 0.5f),
                    new Vector2(delta.Length(), BodyCurveWidth), SpriteEffects.None);
            }
        }

        private void DrawNodes(Texture2D texture, IReadOnlyList<Vector2> points, SpriteEffects effects)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 delta = points[i + 1] - points[i];
                Rectangle frame = i == 0 ? GetHandleFrame(texture) : GetNodeFrame(texture, i);
                float scale = Projectile.scale * (i == 0 ? Settings.HandleScale : Settings.NodeScale);
                Color nodeColor = Lighting.GetColor(points[i].ToTileCoordinates());
                if (Settings.NodesGlow)
                    nodeColor = Color.Lerp(nodeColor, NodeGlowColor, NodeGlowStrength);
                Main.EntitySpriteDraw(texture, points[i] - Main.screenPosition, frame,
                    nodeColor, delta.ToRotation() - MathHelper.PiOver2,
                    frame.Size() * 0.5f, scale, effects);
            }
        }

        private static Color MultiplyByLighting(Color tint, Vector2 worldPosition)
        {
            Color ambient = Lighting.GetColor(worldPosition.ToTileCoordinates());
            Vector3 rgb = tint.ToVector3() * ambient.ToVector3();
            return new Color(rgb) { A = tint.A };
        }
    }
}
