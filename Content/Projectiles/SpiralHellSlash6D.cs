using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    // Excalibur-style close-range energy swing using the cyan _6d texture.
    public sealed class SpiralHellSlash6D : ModProjectile
    {
        private static readonly Color DarkCyan = new(0, 115, 105);
        private static readonly Color BrightCyan = new(0, 255, 230);
        private static readonly Color PaleCyan = new(170, 255, 248);

        public override string Texture => "everflow/Content/Projectiles/SpiralHellProjectile_6d";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.ownerHitCheckDistance = 300f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.stopsDealingDamageAfterPenetrateHits = true;
            Projectile.noEnchantmentVisuals = true;
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || Projectile.ai[1] <= 0f)
            {
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0]++;
            float progress = Projectile.localAI[0] / Projectile.ai[1];
            float swingDirection = Projectile.ai[0];
            Projectile.rotation = MathHelper.Pi * swingDirection * progress
                + Projectile.velocity.ToRotation()
                + swingDirection * MathHelper.Pi
                + player.fullRotation;
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter) - Projectile.velocity;
            Projectile.scale = (1f + progress * 0.6f) * Projectile.ai[2];

            float dustRotation = Projectile.rotation
                + Main.rand.NextFloatDirection() * MathHelper.PiOver2 * 0.7f;
            Vector2 edge = Projectile.Center + dustRotation.ToRotationVector2() * 84f * Projectile.scale;
            Vector2 dustVelocity = (dustRotation + swingDirection * MathHelper.PiOver2).ToRotationVector2();

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + dustRotation.ToRotationVector2()
                        * Main.rand.NextFloat(20f, 100f) * Projectile.scale,
                    DustID.FireworksRGB,
                    dustVelocity,
                    100,
                    Color.Lerp(BrightCyan, Color.White, Main.rand.NextFloat(0.3f)),
                    0.45f);
                dust.noGravity = true;
                dust.fadeIn = Main.rand.NextFloat(0.4f, 0.55f);
            }

            if (Main.rand.NextBool(2))
                Dust.NewDustPerfect(edge, DustID.TintableDustLighted, dustVelocity, 100, PaleCyan, 1.1f);

            Lighting.AddLight(edge, BrightCyan.ToVector3() * 0.55f);
            if (Projectile.localAI[0] >= Projectile.ai[1])
                Projectile.Kill();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float coneLength = 94f * Projectile.scale;
            float coneRotation = Projectile.rotation + MathHelper.TwoPi / 25f * Projectile.ai[0];
            const float maximumAngle = MathHelper.PiOver4;
            if (targetHitbox.IntersectsConeSlowMoreAccurate(
                Projectile.Center, coneLength, coneRotation, maximumAngle))
                return true;

            float backOfSwing = Utils.Remap(
                Projectile.localAI[0], Projectile.ai[1] * 0.3f, Projectile.ai[1] * 0.5f, 1f, 0f);
            if (backOfSwing > 0f)
            {
                float secondRotation = coneRotation
                    - MathHelper.PiOver4 * Projectile.ai[0] * backOfSwing;
                if (targetHitbox.IntersectsConeSlowMoreAccurate(
                    Projectile.Center, coneLength, secondRotation, maximumAngle))
                    return true;
            }
            return false;
        }

        public override void CutTiles()
        {
            Vector2 start = (Projectile.rotation - MathHelper.PiOver4).ToRotationVector2()
                * 60f * Projectile.scale;
            Vector2 end = (Projectile.rotation + MathHelper.PiOver4).ToRotationVector2()
                * 60f * Projectile.scale;
            Utils.PlotTileLine(Projectile.Center + start, Projectile.Center + end,
                60f * Projectile.scale, DelegateMethods.CutTiles);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<SpiralHellNightsEdgeHit>(),
                0,
                0f,
                Projectile.owner,
                Projectile.rotation,
                Projectile.ai[0]);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Rectangle baseFrame = texture.Frame(1, 4, 0, 0);
            Rectangle shineFrame = texture.Frame(1, 4, 0, 3);
            Vector2 origin = baseFrame.Size() * 0.5f;
            float progress = Projectile.localAI[0] / Projectile.ai[1];
            float visibility = Utils.Remap(progress, 0f, 0.6f, 0f, 1f)
                * Utils.Remap(progress, 0.6f, 1f, 1f, 0f);
            float scale = Projectile.scale * 1.1f;
            SpriteEffects effects = Projectile.ai[0] < 0f
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None;

            Main.EntitySpriteDraw(texture, position, baseFrame, DarkCyan * visibility,
                Projectile.rotation - Projectile.ai[0] * MathHelper.PiOver4 * (1f - progress),
                origin, scale, effects);
            Main.EntitySpriteDraw(texture, position, baseFrame, BrightCyan * visibility * 0.55f,
                Projectile.rotation, origin, scale, effects);
            Main.EntitySpriteDraw(texture, position, baseFrame, PaleCyan * visibility * 0.65f,
                Projectile.rotation, origin, scale * 0.975f, effects);
            Main.EntitySpriteDraw(texture, position, shineFrame, Color.White * visibility * 0.7f,
                Projectile.rotation + Projectile.ai[0] * 0.01f, origin, scale, effects);
            Main.EntitySpriteDraw(texture, position, shineFrame, Color.White * visibility * 0.5f,
                Projectile.rotation - Projectile.ai[0] * 0.05f, origin, scale * 0.8f, effects);
            Main.EntitySpriteDraw(texture, position, shineFrame, Color.White * visibility * 0.35f,
                Projectile.rotation - Projectile.ai[0] * 0.1f, origin, scale * 0.6f, effects);
            return false;
        }
    }
}
