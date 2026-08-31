using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    /// <summary>
    /// Friendly magic counterpart of Retinazer's red eye laser.
    /// </summary>
    public sealed class SpiralHellProjectile3 : ModProjectile
    {
        private static readonly Color LaserColor = new Color(255, 45, 45);

        public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.EyeLaser}";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.scale = 3f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ArmorPenetration = SpiralHellForm.GetFormStats(3).ArmorPenetration;
            Projectile.penetrate = SpiralHellForm.GetFormStats(3).Penetration + 1;
            Projectile.timeLeft = 600;
            // The scope round gains wall penetration together with Prosperito
            // at stage 4. In stages 2-3 it still collides with terrain.
            Projectile.tileCollide = SpiralHellForm.GetProgressionStage() < 4;
            Projectile.ignoreWater = true;
            Projectile.alpha = 0;
            // Ten movement/collision updates per game tick. This preserves
            // reliable hit detection while giving an effective speed of 160.
            Projectile.extraUpdates = 9;
        }

        public override void AI()
        {
            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Lighting.AddLight(Projectile.Center, 3f, 0.12f, 0.12f);
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // Keep the projectile self-lit like Barricado's stars, but tint
            // the Eye Laser texture strongly red so its native purple/blue
            // channels cannot show through in bright environments.
            return new Color(255, 24, 24, 255);
        }
    }
}
