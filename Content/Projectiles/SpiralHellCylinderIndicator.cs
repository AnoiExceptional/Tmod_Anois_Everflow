using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>
    /// Purely visual cylinder indicator used while Spiral Hell changes form.
    /// The ammunition and chamber are separate projectiles so the chamber can
    /// mask the ammunition exactly like a real revolver cylinder.
    /// </summary>
    public abstract class SpiralHellCylinderIndicator : ModProjectile
    {
        private const int HoldDuration = 90;

        protected abstract bool RotatesWithAmmunition { get; }

        private int StartForm => System.Math.Clamp((int)Projectile.ai[0], 1, 6);
        protected int EndForm => System.Math.Clamp((int)Projectile.ai[1], 1, 6);
        private int Duration => System.Math.Max(1, (int)Projectile.ai[2]);

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
            Projectile.timeLeft = 100;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.MountedCenter - Vector2.UnitY * 80f;
            Projectile.velocity = Vector2.Zero;

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.timeLeft = Duration + HoldDuration;
            }

            if (!RotatesWithAmmunition)
            {
                Projectile.rotation = 0f;
                return;
            }

            float chamberAngle = MathHelper.TwoPi / 6f;
            float startRotation = -(StartForm - 1) * chamberAngle;
            float endRotation = -(EndForm - 1) * chamberAngle;

            // Returning to Rotatio passes every skipped locked chamber at high
            // speed instead of taking the visually shortest clockwise path.
            if (EndForm == 1)
                endRotation = -MathHelper.TwoPi;

            float elapsed = Duration + HoldDuration - Projectile.timeLeft;
            float progress = MathHelper.Clamp(
                elapsed / System.Math.Max(1f, Duration - 1f), 0f, 1f);
            Projectile.rotation = MathHelper.Lerp(startRotation, endRotation,
                MathHelper.SmoothStep(0f, 1f, progress));
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            Main.EntitySpriteDraw(texture,
                drawPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None);
            return false;
        }

        protected static Color GetFormThemeColor(int form) => form switch
        {
            1 => new Color(0x5A, 0xFF, 0x78),
            2 => new Color(0x5F, 0x87, 0xFF),
            3 => new Color(0xFF, 0x52, 0x52),
            4 => new Color(0xFF, 0xF0, 0x56),
            5 => new Color(0xCA, 0x6F, 0xFF),
            6 => new Color(0x00, 0xFF, 0xE6),
            _ => Color.White
        };
    }

    public sealed class SpiralHellCylinderAmmunition : SpiralHellCylinderIndicator
    {
        public override string Texture =>
            "everflow/Content/Projectiles/SpiralHell_Cylinder_2";

        protected override bool RotatesWithAmmunition => true;
    }

    public sealed class SpiralHellCylinderChamber : SpiralHellCylinderIndicator
    {
        public override string Texture =>
            "everflow/Content/Projectiles/SpiralHell_Cylinder_1";

        protected override bool RotatesWithAmmunition => false;
    }

    public sealed class SpiralHellCylinderTentacles : SpiralHellCylinderIndicator
    {
        public override string Texture =>
            $"Terraria/Images/Projectile_{ProjectileID.ShadowFlame}";

        protected override bool RotatesWithAmmunition => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[
                ProjectileID.ShadowFlame].Value;
            int frameCount = System.Math.Max(1, Main.projFrames[ProjectileID.ShadowFlame]);
            Rectangle frame = texture.Frame(1, frameCount, 0, 0);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color color = Color.Lerp(GetFormThemeColor(EndForm), Color.White, 0.22f);
            color.A = 0;
            float time = Main.GlobalTimeWrappedHourly;
            float overallPulse = 0.88f
                + 0.08f * (float)System.Math.Sin(time * 7.1f + Projectile.identity * 0.37f)
                + 0.04f * (float)System.Math.Sin(time * 13.7f + Projectile.identity * 0.19f);

            // Shadowflame Hex Doll tentacles are built by repeatedly laying
            // the tentacle projectile texture along their path. Keep that
            // segmented construction, but constrain the path to two straight
            // horizontal arms and taper their outer ends.
            const int SegmentCount = 18;
            const float SegmentSpacing = 4.5f;
            for (int direction = -1; direction <= 1; direction += 2)
            {
                for (int segment = 0; segment < SegmentCount; segment++)
                {
                    // Begin at the cylinder center. The ammunition and chamber
                    // layers naturally mask the inner portion of each arm.
                    float distance = segment * SegmentSpacing;
                    float taper = MathHelper.Lerp(0.4125f, 0.1875f,
                        segment / (SegmentCount - 1f));
                    float localPulse = 0.94f + 0.06f * (float)System.Math.Sin(
                        time * 10.3f - segment * 0.72f + direction * 0.9f);
                    float brightness = overallPulse * localPulse;
                    float breathingScale = 1f + 0.035f * (float)System.Math.Sin(
                        time * 8.6f - segment * 0.55f + direction);
                    taper *= breathingScale;
                    Vector2 position = center + Vector2.UnitX * direction * distance;
                    float rotation = direction > 0 ? MathHelper.PiOver2 : -MathHelper.PiOver2;

                    // Closely overlapped segments remove the original dotted
                    // appearance. Two offset rings provide soft bloom around
                    // the full-strength energy body without lighting tiles.
                    for (int glow = 0; glow < 8; glow++)
                    {
                        Vector2 glowOffset = Vector2.UnitX.RotatedBy(
                            MathHelper.TwoPi * glow / 8f) * 3f;
                        Main.EntitySpriteDraw(texture, position + glowOffset, frame,
                            color * (0.10f * brightness), rotation, origin, taper * 1.55f,
                            SpriteEffects.None);
                    }

                    for (int glow = 0; glow < 4; glow++)
                    {
                        Vector2 glowOffset = Vector2.UnitX.RotatedBy(
                            MathHelper.TwoPi * glow / 4f) * 6f;
                        Main.EntitySpriteDraw(texture, position + glowOffset, frame,
                            color * (0.045f * brightness), rotation, origin, taper * 1.8f,
                            SpriteEffects.None);
                    }

                    Main.EntitySpriteDraw(texture, position, frame,
                        color * (0.28f * brightness), rotation, origin, taper * 1.35f,
                        SpriteEffects.None);
                    Main.EntitySpriteDraw(texture, position, frame,
                        color * (0.82f * brightness), rotation, origin, taper,
                        SpriteEffects.None);
                }
            }

            return false;
        }
    }
}
