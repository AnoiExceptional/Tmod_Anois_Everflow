using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public class AlloyBowThrown : ModProjectile
    {
        // 使用 Alloy Bow 本体贴图
        public override string Texture => "everflow/Content/Items/AlloyBow";

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.WoodenBoomerang);

            Projectile.aiStyle = ProjAIStyleID.Boomerang;
            AIType = ProjectileID.WoodenBoomerang;

            Projectile.DamageType = DamageClass.Ranged;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;

            // Alloy Bow 实际贴图 16x64
            // 因此旋转中心严格为 (8, 32)
            Vector2 origin = new Vector2(
                texture.Width / 2f,
                texture.Height / 2f
            );

            Vector2 drawPosition =
                Projectile.Center - Main.screenPosition;

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0
            );

            // 禁止 Terraria 再绘制一次默认 projectile
            return false;
        }
    }
}