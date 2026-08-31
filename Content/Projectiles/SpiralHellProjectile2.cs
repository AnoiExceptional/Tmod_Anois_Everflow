using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    public sealed class SpiralHellProjectile2 : ModProjectile
    {
        private static readonly Color ProjectileColor = new Color(95, 135, 255);

        public override string Texture => "everflow/Content/Projectiles/SpiralHellProjectile_2";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = SpiralHellForm.GetFormStats(2).Penetration + 1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            // Apply gravity up to terminal fall speed, but never reduce an
            // initial downward velocity that is already faster than it. The
            // previous unconditional Min flattened steep downward shots by
            // cutting only their Y velocity on the first update.
            if (Projectile.velocity.Y < 16f)
                Projectile.velocity.Y = MathHelper.Min(Projectile.velocity.Y + 0.22f, 16f);
            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, ProjectileColor.ToVector3() * 0.65f);

            // Roughly two particles every three ticks: visible without becoming
            // a dense trail at the weapon's automatic fire rate.
            if (Main.rand.NextBool(2, 3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * 5f,
                    DustID.RainbowTorch,
                    -Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    80,
                    ProjectileColor,
                    Main.rand.NextFloat(0.8f, 1.15f));
                dust.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.65f }, Projectile.Center);

            Vector2 center = Projectile.Center;

            // Lunar Flare's explosion is the original projectile playing a
            // seven-frame animation. Use a separate, harmless visual here so
            // Impacto keeps its existing explosion damage and penetration rules.
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_Death(),
                    center,
                    Vector2.Zero,
                    ModContent.ProjectileType<SpiralHellImpactoLunarExplosion>(),
                    0,
                    0f,
                    Projectile.owner,
                    ai0: SpiralHellForm.GetFormStats(2).ExplosionSize);
            }

            int explosionSize = SpiralHellForm.GetFormStats(2).ExplosionSize;
            Projectile.Resize(explosionSize, explosionSize);
            Projectile.Center = center;
            Projectile.Damage();

            for (int i = 0; i < 28; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(4.5f, 4.5f)
                    * Main.rand.NextFloat(0.45f, 1f);
                Dust dust = Dust.NewDustPerfect(
                    center,
                    DustID.RainbowTorch,
                    velocity,
                    55,
                    ProjectileColor,
                    Main.rand.NextFloat(1f, 1.55f));
                dust.noGravity = true;
            }

            Lighting.AddLight(center, ProjectileColor.ToVector3() * 1.2f);
        }
    }
}
