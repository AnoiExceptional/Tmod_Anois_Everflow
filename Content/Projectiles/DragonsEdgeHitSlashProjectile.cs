using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>
    /// Purely visual True Night's Edge-style cross slash spawned on hit.
    /// ai[0] = 0 uses the melee swing color; ai[0] = 1 uses the blade-wave color.
    /// ai[1] stores a small per-hit random rotation offset.
    /// </summary>
    public sealed class DragonsEdgeHitSlashProjectile : ModProjectile
    {
        private const int Lifetime = 16;
        private static readonly Color MeleeColor = new(255, 64, 182);
        private static readonly Color BladeWaveColor = new(246, 162, 168);

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

        private Color EffectColor => Projectile.ai[0] >= 0.5f
            ? BladeWaveColor
            : MeleeColor;

        private float Progress => 1f - Projectile.timeLeft / (float)Lifetime;

        private float Visibility
        {
            get
            {
                float progress = Progress;
                return Utils.Remap(progress, 0f, 0.12f, 0f, 1f)
                    * Utils.Remap(progress, 0.42f, 1f, 1f, 0f);
            }
        }

        public override void AI()
        {
            float visibility = Visibility;
            Lighting.AddLight(
                Projectile.Center,
                EffectColor.ToVector3() * visibility * 0.6f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(1.3f, 1.3f),
                    80,
                    EffectColor,
                    Main.rand.NextFloat(0.55f, 0.85f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = EffectColor;
            float visibility = Visibility;

            DrawPrettyStarSparkle(
                1f,
                SpriteEffects.None,
                Projectile.Center - Main.screenPosition,
                new Color(255, 255, 255, 0) * visibility * 0.5f,
                color * visibility,
                Progress,
                0f,
                0.12f,
                0.42f,
                1f,
                MathHelper.PiOver4 + Projectile.ai[1],
                new Vector2(2f, 2f),
                Vector2.One);

            return false;
        }

        private static void DrawPrettyStarSparkle(
            float opacity,
            SpriteEffects effects,
            Vector2 drawPosition,
            Color drawColor,
            Color shineColor,
            float flareCounter,
            float fadeInStart,
            float fadeInEnd,
            float fadeOutStart,
            float fadeOutEnd,
            float rotation,
            Vector2 scale,
            Vector2 fatness)
        {
            Texture2D texture = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            // Compared with the vanilla cross, make the whole glint 25%
            // brighter while keeping its hue intact.
            Color largeColor = shineColor * opacity * 0.625f;
            largeColor.A = 0;
            Color smallColor = drawColor * 0.625f;
            Vector2 origin = texture.Size() * 0.5f;
            float visibility = Utils.GetLerpValue(
                    fadeInStart, fadeInEnd, flareCounter, true)
                * Utils.GetLerpValue(
                    fadeOutEnd, fadeOutStart, flareCounter, true);
            Vector2 horizontalScale = new(fatness.X * 0.5f, scale.X);
            Vector2 verticalScale = new(fatness.Y * 0.5f, scale.Y);
            // Shorten one of the two crossing cuts by 50%; the perpendicular
            // cut retains the original True Night's Edge proportions.
            horizontalScale.Y *= 0.5f;
            horizontalScale *= visibility;
            verticalScale *= visibility;
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
