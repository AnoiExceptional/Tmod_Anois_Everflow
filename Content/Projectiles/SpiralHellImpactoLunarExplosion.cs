using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>
    /// Harmless visual copy of Lunar Flare's seven-frame explosion animation.
    /// Impacto's actual area damage remains in SpiralHellProjectile2.OnKill.
    /// </summary>
    public sealed class SpiralHellImpactoLunarExplosion : ModProjectile
    {
        private static readonly Color ImpactoColor = new Color(95, 135, 255);
        private const float LunarFlareFrameSize = 98f;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LunarFlare}";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = 140;
            Projectile.height = 140;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.timeLeft = 21;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            // ai[0] receives the current progression stage's real Impacto
            // explosion diameter. Scale the 98x98 vanilla animation to match it.
            int explosionSize = System.Math.Max(1, (int)Projectile.ai[0]);
            if (Projectile.width != explosionSize || Projectile.height != explosionSize)
            {
                Vector2 center = Projectile.Center;
                Projectile.Resize(explosionSize, explosionSize);
                Projectile.Center = center;
            }

            Projectile.scale = explosionSize / LunarFlareFrameSize;

            // Vanilla reduces alpha by 10 on each of six updates per tick.
            Projectile.alpha = System.Math.Max(0, Projectile.alpha - 60);

            float visibility = 1f - Projectile.alpha / 255f;
            Lighting.AddLight(
                Projectile.Center,
                new Vector3(0.12f, 0.28f, 1f) * (0.95f * visibility));

            // Vanilla holds each of the seven frames for three game ticks.
            if (++Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Type])
                    Projectile.Kill();
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            int alpha = Projectile.alpha;
            float visibility = 1f - alpha / 255f;
            return new Color(
                (int)(ImpactoColor.R * visibility),
                (int)(ImpactoColor.G * visibility),
                (int)(ImpactoColor.B * visibility),
                127 - alpha / 2);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.LunarFlare].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = frame.Size() * 0.5f;
            float visibility = 1f - Projectile.alpha / 255f;
            float drawVisibility = visibility * 0.5f;
            float pulse = 1f + 0.025f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 12f);

            // Draw from the exact center of the current 98x98 animation frame.
            // The default projectile draw path uses offsets derived from the
            // dynamically resized hitbox, which displaced the scaled effect.
            // Several differently-sized passes preserve deep shadows while
            // adding a cyan-white refractive core, similar to a crystal full of
            // churning magical energy instead of a flat cloud of blue smoke.
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                frame,
                new Color(8, 16, 72, (int)(190f * drawVisibility)),
                0f,
                origin,
                Projectile.scale * 1.07f,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                frame,
                new Color(
                    (int)(ImpactoColor.R * drawVisibility),
                    (int)(ImpactoColor.G * drawVisibility),
                    (int)(ImpactoColor.B * drawVisibility),
                    (int)(155f * drawVisibility)),
                0f,
                origin,
                Projectile.scale * pulse,
                SpriteEffects.None,
                0f);

            // Alpha zero under Terraria's premultiplied blending produces an
            // additive highlight without washing the dark blue body away.
            Color crystalHighlight = new Color(
                (int)(90f * drawVisibility),
                (int)(205f * drawVisibility),
                (int)(255f * drawVisibility),
                0);
            Vector2 refractionOffset = new Vector2(1.5f, -1f) * Projectile.scale;

            Main.EntitySpriteDraw(
                texture,
                drawPosition + refractionOffset,
                frame,
                crystalHighlight,
                0f,
                origin,
                Projectile.scale * 0.93f,
                SpriteEffects.None,
                0f);

            Main.EntitySpriteDraw(
                texture,
                drawPosition - refractionOffset * 0.45f,
                frame,
                new Color(
                    (int)(185f * drawVisibility),
                    (int)(225f * drawVisibility),
                    (int)(255f * drawVisibility),
                    0),
                0f,
                origin,
                Projectile.scale * 0.78f,
                SpriteEffects.None,
                0f);

            return false;
        }
    }
}
