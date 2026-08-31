using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    public sealed class SpiralHellPrismBeam : ModProjectile
    {
        private const float BeamLength = 2400f;
        private const float MuzzleDistance = 56f;
        private const float BeamWidth = 20f;
        private static readonly Color BeamColor = new Color(202, 111, 255);
        private static readonly SoundStyle ChargeLoopSound = SoundID.Item13 with
        {
            IsLooped = true,
            Volume = 0.75f,
            PitchVariance = 0f,
            MaxInstances = 4,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused
        };
        private static readonly SoundStyle BeamLoopSound = SoundID.Item34 with
        {
            IsLooped = true,
            Volume = 1f,
            PitchVariance = 0f,
            MaxInstances = 4,
            SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
            PauseBehavior = PauseBehavior.StopWhenGamePaused
        };

        private SlotId loopSoundSlot;
        private bool playingBeamSound;

        private ref float Charge => ref Projectile.ai[0];
        private int ChargeTime => SpiralHellForm.GetFormStats(5).ChargeTime;
        private int ManaCost => SpiralHellForm.GetFormStats(5).Mana;
        private bool IsFiring => Charge >= ChargeTime;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.LastPrismLaser}";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => IsFiring ? null : false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed || !player.channel)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Vector2 center = player.RotatedRelativePoint(player.MountedCenter);

            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 desiredDirection = (Main.MouseWorld - center)
                    .SafeNormalize(Vector2.UnitX * player.direction);
                float newAngle = Utils.AngleLerp(
                    Projectile.velocity.ToRotation(),
                    desiredDirection.ToRotation(),
                    0.065f);
                Projectile.velocity = newAngle.ToRotationVector2();
                Projectile.netUpdate = true;
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.Center = center;
            Projectile.rotation = direction.ToRotation();
            player.ChangeDir(direction.X >= 0f ? 1 : -1);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            // Flying mounts such as Witch's Broom and the UFO rotate the
            // entire player through fullRotation, with an angle that changes
            // continuously with horizontal speed. The held-item renderer adds
            // that body rotation after itemRotation, while the beam remains in
            // world-space. Cancel the live mount tilt so the weapon texture
            // stays aligned with the projectile at every speed.
            player.itemRotation = direction.ToRotation() - player.fullRotation;
            if (player.direction < 0)
                player.itemRotation -= MathHelper.Pi;

            Charge++;
            UpdateLoopedSound(center + direction * MuzzleDistance);

            if (Projectile.owner == Main.myPlayer && Charge % 10f == 0f)
            {
                if (!player.CheckMana(ManaCost, true))
                {
                    Projectile.Kill();
                    return;
                }
            }

            Vector2 muzzle = center + direction * MuzzleDistance;
            if (!IsFiring)
            {
                float chargeRatio = Charge / ChargeTime;
                Lighting.AddLight(muzzle, BeamColor.ToVector3() * chargeRatio * 0.8f);
                return;
            }

            for (float distance = 0f; distance <= BeamLength; distance += 64f)
            {
                Vector2 point = muzzle + direction * distance;
                Lighting.AddLight(point, BeamColor.ToVector3() * 1.35f);
            }
        }

        private void UpdateLoopedSound(Vector2 soundPosition)
        {
            bool shouldPlayBeamSound = IsFiring;

            if (SoundEngine.TryGetActiveSound(loopSoundSlot, out ActiveSound activeSound))
            {
                if (playingBeamSound != shouldPlayBeamSound)
                {
                    activeSound.Stop();
                }
                else
                {
                    activeSound.Position = soundPosition;

                    if (shouldPlayBeamSound)
                    {
                        // Fade the Elf Melter loop in over eight frames to
                        // avoid a hard waveform edge at the phase transition.
                        float fadeIn = MathHelper.Clamp((Charge - ChargeTime) / 8f, 0f, 1f);
                        activeSound.Volume = fadeIn;
                    }
                    else
                    {
                        // Fade the charge loop during its final eight frames.
                        float fadeOut = MathHelper.Clamp((ChargeTime - Charge) / 8f, 0f, 1f);
                        activeSound.Volume = 0.75f * fadeOut;
                    }

                    return;
                }
            }

            playingBeamSound = shouldPlayBeamSound;
            SoundStyle style = shouldPlayBeamSound ? BeamLoopSound : ChargeLoopSound;
            ProjectileAudioTracker tracker = new ProjectileAudioTracker(Projectile);
            loopSoundSlot = SoundEngine.PlaySound(
                style,
                soundPosition,
                soundInstance =>
                {
                    soundInstance.Position = Projectile.Center;
                    return tracker.IsActiveAndInGame();
                });

            // Begin the beam loop silently; the next updates fade it in.
            if (shouldPlayBeamSound
                && SoundEngine.TryGetActiveSound(loopSoundSlot, out ActiveSound newBeamSound))
            {
                newBeamSound.Volume = 0f;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (SoundEngine.TryGetActiveSound(loopSoundSlot, out ActiveSound activeSound))
                activeSound.Stop();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!IsFiring)
                return false;

            Player player = Main.player[Projectile.owner];
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 start = player.RotatedRelativePoint(player.MountedCenter) + direction * MuzzleDistance;
            Vector2 end = start + direction * BeamLength;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                start,
                end,
                BeamWidth,
                ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 muzzle = player.RotatedRelativePoint(player.MountedCenter)
                + direction * MuzzleDistance;
            Vector2 lineStart = muzzle - Main.screenPosition;
            Vector2 lineEnd = lineStart + direction * BeamLength;

            if (!IsFiring)
            {
                float chargeRatio = MathHelper.Clamp(Charge / ChargeTime, 0f, 1f);
                Color guideColor = BeamColor * (0.15f + chargeRatio * 0.35f);
                guideColor.A = 64;
                DrawPrismLaser(lineStart, lineEnd, new Vector2(0.12f + chargeRatio * 0.13f), guideColor);
                return false;
            }

            Color outerColor = BeamColor;
            outerColor.A = 64;
            Color innerColor = new Color(245, 210, 255, 64);
            DrawPrismLaser(lineStart, lineEnd, new Vector2(1.45f), outerColor * 0.8f);
            DrawPrismLaser(lineStart, lineEnd, new Vector2(0.72f), innerColor * 0.9f);
            return false;
        }

        private static void DrawPrismLaser(
            Vector2 startPosition,
            Vector2 endPosition,
            Vector2 drawScale,
            Color beamColor)
        {
            // This is the same start/body/end framing pipeline used by the
            // vanilla Last Prism and tModLoader's ExampleLastPrismBeam.
            Texture2D texture = TextureAssets.Projectile[ProjectileID.LastPrismLaser].Value;
            DelegateMethods.f_1 = 1f;
            DelegateMethods.c_1 = beamColor;
            Utils.LaserLineFraming lineFraming = new Utils.LaserLineFraming(
                DelegateMethods.RainbowLaserDraw);
            Utils.DrawLaser(
                Main.spriteBatch,
                texture,
                startPosition,
                endPosition,
                drawScale,
                lineFraming);
        }
    }
}
