using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public abstract class SpiralHellStar6Base : ModProjectile
    {
        private static readonly Color StarColor = new Color(0, 255, 230);
        private bool shatteredByEnemy;

        protected virtual int HitboxSize => 18;

        public override void SetDefaults()
        {
            Projectile.width = HitboxSize;
            Projectile.height = HitboxSize;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 toPlayer = owner.Center - Projectile.Center;
            float distance = toPlayer.Length();
            // 5 tiles (80 px) = 0 mph; 30 tiles (480 px) = 24 mph.
            // Terraria's stopwatch conversion is 5 mph per pixel/tick, so
            // 24 mph corresponds to 4.8 pixels/tick.
            float speedRatio = MathHelper.Clamp((distance - 80f) / 400f, 0f, 1f);
            float speed = MathHelper.Lerp(0f, 4.8f, speedRatio);
            Projectile.velocity = toPlayer.SafeNormalize(Vector2.Zero) * speed;
            Projectile.rotation += 0.055f;

            Lighting.AddLight(Projectile.Center, StarColor.ToVector3() * 0.25f);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // Draw the star at full texture brightness regardless of ambient
            // darkness. This does not change the light it casts on the world.
            return Color.White;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            shatteredByEnemy = true;
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (!shatteredByEnemy)
                return;

            for (int i = 0; i < 30; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(5f, 5f)
                    * Main.rand.NextFloat(0.3f, 1f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.RainbowTorch,
                    velocity,
                    35,
                    StarColor,
                    Main.rand.NextFloat(0.9f, 1.45f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 0f, 2.2f, 2f);
        }
    }

    public sealed class SpiralHellStar6A : SpiralHellStar6Base
    {
        public override string Texture => "everflow/Content/Projectiles/SpiralHellProjectile_6a";
    }

    public sealed class SpiralHellStar6B : SpiralHellStar6Base
    {
        public override string Texture => "everflow/Content/Projectiles/SpiralHellProjectile_6b";
        protected override int HitboxSize => 24;
    }

    public sealed class SpiralHellStar6C : SpiralHellStar6Base
    {
        public override string Texture => "everflow/Content/Projectiles/SpiralHellProjectile_6c";
        protected override int HitboxSize => 30;
    }
}
