using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class AncientTroopGrenade : ModProjectile
    {
        private const int FuseTime = 2 * 60;
        private const int ExplosionSize = 128;

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Grenade}";

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Grenade);
            AIType = ProjectileID.Grenade;

            // The thrown body is harmless. A vanilla grenade explosion proxy
            // applies damage only when the grenade actually detonates.
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.ArmorPenetration = 0;
            Projectile.timeLeft = FuseTime;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Projectile.tileCollide = true;
        }

        public override void AI()
        {
            if (Projectile.timeLeft > 3)
                return;

            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.penetrate = -1;
            // This is an enemy explosion, not a player-owned vanilla grenade hit.
            // Keeping it hostile-only makes player defense participate in the
            // normal hostile-projectile damage calculation.
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.ArmorPenetration = 0;
            Projectile.Resize(ExplosionSize, ExplosionSize);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            for (int i = 0; i < 30; i++)
            {
                Dust smoke = Dust.NewDustDirect(
                    Projectile.Center - new Vector2(32f),
                    64,
                    64,
                    DustID.Smoke,
                    Scale: Main.rand.NextFloat(1.2f, 2f));
                smoke.velocity *= 1.8f;
            }

            for (int i = 0; i < 40; i++)
            {
                Dust fire = Dust.NewDustDirect(
                    Projectile.Center - new Vector2(32f),
                    64,
                    64,
                    DustID.Torch,
                    Scale: Main.rand.NextFloat(1.3f, 2.2f));
                fire.noGravity = true;
                fire.velocity *= 2.5f;
            }
        }
    }
}
