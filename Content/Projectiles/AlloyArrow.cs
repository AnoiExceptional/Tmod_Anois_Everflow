using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public class AlloyArrow : ModProjectile
    {
        public override string Texture =>
            "everflow/Content/Projectiles/AlloyXbowProjectile";

        public override void SetStaticDefaults()
        {
            // 骨箭使用短距离位置缓存形成残影。
            ProjectileID.Sets.TrailCacheLength[Type] = 5;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.BoneArrow);
            AIType = ProjectileID.BoneArrow;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 5;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float opacity =
                    (Projectile.oldPos.Length - i) /
                    (float)(Projectile.oldPos.Length + 1) * 0.45f;

                // TrailingMode 0 主要缓存历史位置，oldRot 对克隆的骨箭并不可靠。
                // 根据相邻位置反推该段残影的实际飞行方向；贴图原始朝上，因此加 Pi/2。
                Vector2 newerPosition = i == 0
                    ? Projectile.position
                    : Projectile.oldPos[i - 1];
                Vector2 trailVelocity = newerPosition - Projectile.oldPos[i];
                float trailRotation = trailVelocity.LengthSquared() > 0.001f
                    ? trailVelocity.ToRotation() + MathHelper.PiOver2
                    : Projectile.rotation;

                Main.EntitySpriteDraw(
                    texture,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    frame,
                    lightColor * opacity,
                    trailRotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None);
            }

            return true;
        }
    }
}
