using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    public sealed class SpiralHellProjectile1 : ModProjectile
    {
        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.GreenLaser}";

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = SpiralHellForm.GetFormStats(1).Penetration + 1;
            // Four smaller movement/collision steps per game tick prevent the
            // 30-50 px/tick bullet from skipping tightly packed targets.
            Projectile.extraUpdates = 3;
            Projectile.timeLeft = 2400;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            // extraUpdates repeats movement four times. Divide the stored
            // velocity so the final per-tick speed still matches the table.
            Projectile.velocity *= 0.25f;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.1f, 0.8f, 0.25f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            // On enemy hits, retain the existing convention: the visual's
            // bottom points along the bullet's travel direction.
            SpawnHitEffect(
                target.Center,
                Projectile.velocity.ToRotation() - MathHelper.PiOver2);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                bool collidedX = System.Math.Abs(
                    Projectile.velocity.X - oldVelocity.X) > 0.001f;
                bool collidedY = System.Math.Abs(
                    Projectile.velocity.Y - oldVelocity.Y) > 0.001f;

                Vector2 outwardNormal = Vector2.Zero;
                if (collidedX)
                    outwardNormal.X = -System.Math.Sign(oldVelocity.X);
                if (collidedY)
                    outwardNormal.Y = -System.Math.Sign(oldVelocity.Y);

                // Slope/platform edge fallback: face opposite the incoming
                // shot and quantize to the nearest of eight directions.
                if (outwardNormal == Vector2.Zero)
                    outwardNormal = -oldVelocity.SafeNormalize(Vector2.UnitY);

                float snappedAngle = System.MathF.Round(
                        outwardNormal.ToRotation() / MathHelper.PiOver4)
                    * MathHelper.PiOver4;

                // The sprite's local bottom is the inward end, so its local
                // top extends along the tile surface normal.
                SpawnHitEffect(
                    Projectile.Center,
                    snappedAngle + MathHelper.PiOver2);
            }

            return true;
        }

        private void SpawnHitEffect(Vector2 position, float rotation)
        {
            int effectIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                position,
                Vector2.Zero,
                ModContent.ProjectileType<SpiralHellRotatioExplosion>(),
                0,
                0f,
                Projectile.owner,
                rotation);

            if (effectIndex >= 0 && effectIndex < Main.maxProjectiles)
            {
                Projectile effect = Main.projectile[effectIndex];
                effect.Center = position;
                effect.damage = 0;
                effect.originalDamage = 0;
                effect.friendly = false;
                effect.hostile = false;
                effect.netUpdate = true;
            }
        }
    }
}
