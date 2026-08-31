using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>
    /// A non-damaging, half-scale, Rotatio-colored rendering of vanilla's
    /// DD2ExplosiveTrapT1Explosion animation. ai[0] stores its rotation.
    /// </summary>
    public sealed class SpiralHellRotatioExplosion : ModProjectile
    {
        private static readonly Color RotatioHighlight = new(210, 255, 218, 0);

        public override string Texture =>
            "everflow/Content/Projectiles/SpiralHellRotatioExplosion";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] =
                Main.projFrames[ProjectileID.DD2ExplosiveTrapT1Explosion];
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.scale = 0.5f;
            Projectile.timeLeft = 60;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.rotation = Projectile.ai[0];
            Projectile.alpha = System.Math.Max(0, Projectile.alpha - 25);

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.Kill();
                    return;
                }
            }

            float opacity = 1f - Projectile.alpha / 255f;
            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.14f, 0.41f, 0.18f) * opacity);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameCount = Main.projFrames[Type];
            Rectangle frame = texture.Frame(1, frameCount, 0, Projectile.frame);

            // Vanilla anchors this animation near the bottom of each frame.
            // Keeping that origin lets the bottom follow the bullet direction.
            Vector2 origin = new(frame.Width * 0.5f, frame.Height - 8f);
            Vector2 position = Projectile.Center - Main.screenPosition;
            float opacity = 1f - Projectile.alpha / 255f;

            Main.EntitySpriteDraw(
                texture,
                position,
                frame,
                Color.White * opacity,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                texture,
                position,
                frame,
                RotatioHighlight * opacity * 0.35f,
                Projectile.rotation,
                origin,
                Projectile.scale * 0.92f,
                SpriteEffects.None,
                0f);

            return false;
        }
    }
}
