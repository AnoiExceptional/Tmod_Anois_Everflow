using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>
    /// The sharp impact glint drawn at the end of vanilla
    /// Main.DrawProj_NightsEdge. This intentionally reproduces only the
    /// DrawPrettyStarSparkle layer, not the large curved swing sprite.
    /// </summary>
    public sealed class SpiralHellNightsEdgeHit : ModProjectile
    {
        private const int Lifetime = 16;
        private static readonly Color BrightCyan = new(0, 255, 230);
        private static readonly Color PaleCyan = new(210, 255, 252, 0);
        private static readonly Color DragonsEdgeWhipPink = new(255, 155, 210);
        private static readonly Color PaleWhipPink = new(255, 225, 242, 0);

        private Color BrightColor => Projectile.ai[2] >= 0.5f
            ? DragonsEdgeWhipPink
            : BrightCyan;

        private Color PaleColor => Projectile.ai[2] >= 0.5f
            ? PaleWhipPink
            : PaleCyan;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float visibility = Utils.Remap(progress, 0f, 0.12f, 0f, 1f)
                * Utils.Remap(progress, 0.38f, 1f, 1f, 0f);
            Lighting.AddLight(Projectile.Center,
                BrightColor.ToVector3() * visibility * 0.55f);

            if (Main.rand.NextBool(3))
            {
                Vector2 velocity = Main.rand.NextVector2Circular(1.4f, 1.4f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center,
                    DustID.TintableDustLighted, velocity, 80,
                    BrightColor, Main.rand.NextFloat(0.55f, 0.85f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float visibility = Utils.Remap(progress, 0f, 0.12f, 0f, 1f)
                * Utils.Remap(progress, 0.38f, 1f, 1f, 0f);

            // The same helper and parameter pattern used by vanilla Night's
            // Edge: a long central cutting line crossed by shorter sharp rays.
            // Alpha-zero colors make the bright portions additive-looking.
            DrawPrettyStarSparkle(
                1f,
                SpriteEffects.None,
                Projectile.Center - Main.screenPosition,
                PaleColor * visibility,
                BrightColor * visibility,
                progress,
                0f,
                0.22f,
                0.22f,
                0.85f,
                Projectile.ai[0] + MathHelper.PiOver4,
                new Vector2(1.15f, 2.35f),
                Vector2.One);

            return false;
        }

        // Copied from the private vanilla Main.DrawPrettyStarSparkle helper.
        // It uses the SharpTears texture twice at perpendicular rotations to
        // form a narrow, pointed cutting glint instead of a stretched pixel.
        private static void DrawPrettyStarSparkle(float opacity,
            SpriteEffects effects, Vector2 drawPosition, Color drawColor,
            Color shineColor, float flareCounter, float fadeInStart,
            float fadeInEnd, float fadeOutStart, float fadeOutEnd,
            float rotation, Vector2 scale, Vector2 fatness)
        {
            Texture2D texture = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Color largeColor = shineColor * opacity * 0.5f;
            largeColor.A = 0;
            Color smallColor = drawColor * 0.5f;
            Vector2 origin = texture.Size() * 0.5f;
            float visibility = Utils.GetLerpValue(
                fadeInStart, fadeInEnd, flareCounter, true)
                * Utils.GetLerpValue(
                    fadeOutEnd, fadeOutStart, flareCounter, true);
            Vector2 horizontalScale = new Vector2(fatness.X * 0.5f, scale.X)
                * visibility;
            Vector2 verticalScale = new Vector2(fatness.Y * 0.5f, scale.Y)
                * visibility;
            largeColor *= visibility;
            smallColor *= visibility;

            Main.EntitySpriteDraw(texture, drawPosition, null, largeColor,
                MathHelper.PiOver2 + rotation, origin, horizontalScale, effects);
            Main.EntitySpriteDraw(texture, drawPosition, null, largeColor,
                rotation, origin, verticalScale, effects);
            Main.EntitySpriteDraw(texture, drawPosition, null, smallColor,
                MathHelper.PiOver2 + rotation, origin,
                horizontalScale * 0.6f, effects);
            Main.EntitySpriteDraw(texture, drawPosition, null, smallColor,
                rotation, origin, verticalScale * 0.6f, effects);
        }
    }
}
