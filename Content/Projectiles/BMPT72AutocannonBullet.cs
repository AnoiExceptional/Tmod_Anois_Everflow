using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>以原版高速子弹为基础的BMPT-72专用橙黄色机炮弹。</summary>
    public sealed class BMPT72AutocannonBullet : ModProjectile
    {
        public override string Texture =>
            $"Terraria/Images/Projectile_{ProjectileID.BulletHighVelocity}";

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.BulletHighVelocity);
            AIType = ProjectileID.BulletHighVelocity;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.scale = 1.5f;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 190, 64) * Projectile.Opacity;
        }
    }
}
