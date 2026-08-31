using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class AncientTroopFlame : ModProjectile
    {
        private const float MaximumTravelDistance = 25f * 16f;
        private Vector2 spawnCenter;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Flames}";

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Flames);
            AIType = ProjectileID.Flames;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.width *= 2;
            Projectile.height *= 2;
            Projectile.scale = 2.8f;
        }

        public override void OnSpawn(IEntitySource source)
        {
            spawnCenter = Projectile.Center;
        }

        public override void AI()
        {
            if (Vector2.DistanceSquared(spawnCenter, Projectile.Center) >=
                MaximumTravelDistance * MaximumTravelDistance)
            {
                Projectile.Kill();
                return;
            }

            // Supplement the vanilla flamethrower trail with larger embers so
            // the stream reads as a denser and thicker jet of flame.
            for (int i = 0; i < 4; i++)
            {
                Dust flameDust = Dust.NewDustDirect(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.Torch,
                    Projectile.velocity.X * 0.12f,
                    Projectile.velocity.Y * 0.12f,
                    80,
                    default,
                    Main.rand.NextFloat(2.1f, 2.8f));
                flameDust.noGravity = true;
                flameDust.velocity += Main.rand.NextVector2Circular(1.1f, 1.1f);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool hitHorizontalSurface = Projectile.velocity.Y != oldVelocity.Y;
            bool hitVerticalSurface = Projectile.velocity.X != oldVelocity.X;

            if (hitHorizontalSurface && !hitVerticalSurface && oldVelocity.Y > 0f)
            {
                Projectile.velocity.X = oldVelocity.X;
                Projectile.velocity.Y = 0f;
                Projectile.position.Y -= 2f;
                return false;
            }

            return true;
        }
    }
}
