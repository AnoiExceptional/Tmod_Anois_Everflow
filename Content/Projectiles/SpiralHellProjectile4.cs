using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    public sealed class SpiralHellProjectile4 : ModProjectile
    {
        private static readonly Color ShardColor = new Color(255, 240, 86);

        public override string Texture => "everflow/Content/Projectiles/SpiralHellProjectile_4";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = SpiralHellForm.GetFormStats(4).Penetration + 1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.frame = Utils.Clamp((int)Projectile.ai[0], 0, 4);
        }

        public override void AI()
        {
            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, ShardColor.ToVector3() * 0.75f);
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.6f }, Projectile.Center);

            for (int i = 0; i < 36; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(5.5f, 5.5f)
                    * Main.rand.NextFloat(0.35f, 1f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.RainbowTorch,
                    velocity,
                    35,
                    ShardColor,
                    Main.rand.NextFloat(0.9f, 1.5f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, 2.4f, 2.15f, 0.25f);
        }
    }
}
