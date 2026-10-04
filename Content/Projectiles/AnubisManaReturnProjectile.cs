using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>
    /// 仿照吸血鬼飞刀的 VampireHeal：从被击杀目标处生成一个追踪玩家的
    /// 回流射弹，沿途留下粒子流，抵达玩家时才结算回复。
    /// </summary>
    public sealed class AnubisManaReturnProjectile : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 toOwner = owner.MountedCenter - Projectile.Center;
            float distance = toOwner.Length();
            if (distance <= 18f)
            {
                RestoreMana(owner);
                Projectile.Kill();
                return;
            }

            Vector2 desiredVelocity = toOwner.SafeNormalize(Vector2.Zero) * 18f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.12f);

            // 和吸血鬼飞刀的治疗回流一样，依靠高速追踪射弹沿途持续留下
            // 粒子来形成线，而不是在 PreDraw 中读取一张直线贴图。
            Color blue = new Color(70, 170, 255);
            Vector2 behind = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + behind * i * 4f + Main.rand.NextVector2Circular(1.5f, 1.5f),
                    DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.025f, 0.055f),
                    50,
                    blue,
                    Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.08f, 0.32f, 0.65f);
        }

        private void RestoreMana(Player owner)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int restored = System.Math.Min((int)Projectile.ai[0], owner.statManaMax2 - owner.statMana);
            if (restored <= 0)
                return;

            owner.statMana += restored;
            owner.ManaEffect(restored);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
