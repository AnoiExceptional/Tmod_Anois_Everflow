using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class JadeArrow : ModProjectile
    {
        public override string Texture =>
            "everflow/Content/Projectiles/Jade_Arrow";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.18f, 0.55f, 0.32f);
        }
    }

    public sealed class JadeRocket : ModProjectile
    {
        public override string Texture =>
            "everflow/Content/Projectiles/Jade_Rocket";

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 360;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.3f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Dust trail = Dust.NewDustDirect(
                Projectile.position,
                Projectile.width,
                Projectile.height,
                DustID.GemEmerald,
                -Projectile.velocity.X * 0.08f,
                -Projectile.velocity.Y * 0.08f,
                80,
                default,
                1.1f);
            trail.noGravity = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            SoundEngine.PlaySound(SoundID.Item14, center);

            Projectile.position = center;
            Projectile.width = 64;
            Projectile.height = 64;
            Projectile.Center = center;
            Projectile.penetrate = -1;
            Projectile.Damage();

            for (int i = 0; i < 64; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(8f, 8f);
                Dust dust = Dust.NewDustPerfect(
                    center,
                    i % 3 == 0 ? DustID.Torch : DustID.GemEmerald,
                    velocity,
                    60,
                    new Color(80, 255, 145),
                    Main.rand.NextFloat(1.1f, 2.1f));
                dust.noGravity = Main.rand.NextBool(3);
            }
        }
    }

    public sealed class JadeInkFlame : ModProjectile
    {
        public override string Texture =>
            "everflow/Content/Projectiles/Jade_Flames";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 7;
        }

        public override void SetDefaults()
        {
            // 继承原版火焰的基础碰撞体参数，但自行驱动动画和视觉，
            // 防止原版橙黄色粒子污染墨绿色喷流。
            Projectile.CloneDefaults(ProjectileID.Flames);
            Projectile.aiStyle = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.npcProj = true;
            Projectile.light = 0f;
            Projectile.timeLeft = 72;
            Projectile.alpha = 30;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            // 火焰先展开，随后轻微减速并在末段淡出。
            float ageProgress = MathHelper.Clamp(Projectile.localAI[0] / 28f, 0f, 1f);
            Projectile.scale = MathHelper.Lerp(0.65f, 1.15f, ageProgress);
            if (Projectile.localAI[0] > 8f)
                Projectile.velocity *= 0.985f;
            if (Projectile.timeLeft < 18)
                Projectile.alpha = (int)MathHelper.Lerp(30f, 255f, 1f - Projectile.timeLeft / 18f);

            if (Main.rand.NextBool(3))
            {
                Color inkGreen = new Color(4, 48, 27);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity * 0.35f,
                    DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.08f),
                    100,
                    inkGreen,
                    Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor) =>
            Color.White * Projectile.Opacity;
    }
}
