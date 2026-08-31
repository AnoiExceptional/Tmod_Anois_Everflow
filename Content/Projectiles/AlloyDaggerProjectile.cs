using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Buffs;

namespace everflow.Content.Projectiles
{
    public class AlloyDaggerProjectile : ModProjectile
    {
        private const float StraightRange = 18f * 16f;

        private float DistanceTravelled
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.BoneDagger);
            Projectile.aiStyle = 0;
            AIType = ProjectileID.None;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.maxPenetrate = 1;
        }

        public override void AI()
        {
            DistanceTravelled += Projectile.velocity.Length();

            if (DistanceTravelled >= StraightRange)
            {
                Projectile.velocity.Y = MathHelper.Min(
                    Projectile.velocity.Y + 0.3f,
                    16f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<AlloyDaggerMark>(), 300);
            Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);

            for (int i = 0; i < 8; i++)
            {
                int dustIndex = Dust.NewDust(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    DustID.Titanium,
                    oldVelocity.X * 0.15f,
                    oldVelocity.Y * 0.15f,
                    Scale: Main.rand.NextFloat(0.8f, 1.2f));

                Main.dust[dustIndex].velocity *= 1.35f;
            }

            return true;
        }
    }
}
