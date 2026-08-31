using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public class AlloyBookRubyBolt : ModProjectile
    {
        public override string Texture =>
            $"Terraria/Images/Projectile_{ProjectileID.RubyBolt}";

        private Vector2 Target => new(Projectile.ai[0], Projectile.ai[1]);

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.RubyBolt);
            AIType = ProjectileID.RubyBolt;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
        }

        public override void AI()
        {
            if (!Projectile.tileCollide)
            {
                Vector2 remaining = Target - Projectile.Center;

                // 弹幕抵达或越过光标落点后，才开始与物块碰撞。
                if (Vector2.Dot(remaining, Projectile.velocity) <= 0f ||
                    remaining.LengthSquared() < 24f * 24f)
                {
                    Projectile.tileCollide = true;
                    Projectile.netUpdate = true;
                }
            }
        }
    }
}
