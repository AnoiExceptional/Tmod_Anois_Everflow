using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class WyrmBladePowEnchantedBeam : ModProjectile
    {
        private static readonly Color BeamColor = new(255, 232, 70);

        public override string Texture =>
            "everflow/Content/Projectiles/WyrmBladePowProjectile_4";

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.EnchantedBeam);
            Projectile.aiStyle = 0;
            AIType = ProjectileID.None;
            Projectile.DamageType = DamageClass.Melee;
        }

        public override void AI()
        {
            // 原贴图剑尖朝右上（-45度），因此额外顺时针旋转45度
            // 后即可让剑尖严格沿射弹速度方向飞行。
            Projectile.rotation =
                Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(
                Projectile.Center,
                BeamColor.ToVector3() * 0.45f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity.SafeNormalize(
                        Vector2.UnitX) * 8f,
                    DustID.TintableDustLighted,
                    -Projectile.velocity * 0.06f +
                        Main.rand.NextVector2Circular(0.45f, 0.45f),
                    80,
                    BeamColor,
                    Main.rand.NextFloat(0.75f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White;
    }
}
