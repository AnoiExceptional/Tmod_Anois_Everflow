using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public class ThousandBladesFlyingNodeProjectile : ModProjectile
    {
        private const int FrameWidth = 18;
        private const int FrameHeight = 30;
        private static readonly Color ThemeColor = new(156, 139, 219);

        public override string Texture =>
            "everflow/Content/Projectiles/ThousandBladesProjectile";

        public override void SetStaticDefaults()
        {
            // 与合金箭相同的短距离历史位置缓存。
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 9;
            Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.ArmorPenetration = 10;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 120;
        }

        public override void AI()
        {
            // 原贴图正方向朝上，减去90°后剑尖沿速度方向飞行。
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.55f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameIndex = Math.Clamp((int)Projectile.ai[0], 0, 11);
            Rectangle frame = new(
                frameIndex % 2 * FrameWidth,
                (frameIndex / 2 + 1) * FrameHeight,
                FrameWidth,
                FrameHeight);

            // 复用合金箭的历史位置残影；根据相邻缓存位置重新计算朝向，
            // 避免高速碎刃的oldRot滞后。颜色固定为魔刀千刃主题紫色。
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float opacity =
                    (Projectile.oldPos.Length - i) /
                    (float)(Projectile.oldPos.Length + 1) * 0.45f;
                Vector2 newerPosition = i == 0
                    ? Projectile.position
                    : Projectile.oldPos[i - 1];
                Vector2 trailVelocity = newerPosition - Projectile.oldPos[i];
                float trailRotation = trailVelocity.LengthSquared() > 0.001f
                    ? trailVelocity.ToRotation() - MathHelper.PiOver2
                    : Projectile.rotation;

                Main.EntitySpriteDraw(
                    texture,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    frame,
                    ThemeColor * opacity,
                    trailRotation,
                    frame.Size() * 0.5f,
                    0.5f,
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                ThemeColor,
                Projectile.rotation,
                frame.Size() * 0.5f,
                0.5f,
                SpriteEffects.None);
            return false;
        }
    }
}
