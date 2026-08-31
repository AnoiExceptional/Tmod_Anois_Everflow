using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public class AlloyWandProjectile : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.VilethornBase);
            AIType = ProjectileID.VilethornBase;
            Projectile.DamageType = DamageClass.Magic;
        }
    }

    public class AlloyWandProjectileTip : ModProjectile
    {
        public override string Texture =>
            "everflow/Content/Projectiles/AlloyWandProjectile_2";

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.VilethornTip);
            AIType = ProjectileID.VilethornTip;
            Projectile.DamageType = DamageClass.Magic;
        }
    }

    public class AlloyWandSegmentDraw : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private bool isAlloyWandSegment;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (projectile.ModProjectile is AlloyWandProjectile or AlloyWandProjectileTip)
            {
                isAlloyWandSegment = true;
                return;
            }

            if (source is EntitySource_Parent parentSource &&
                parentSource.Entity is Projectile parent &&
                parent.GetGlobalProjectile<AlloyWandSegmentDraw>().isAlloyWandSegment)
            {
                isAlloyWandSegment = true;
            }
        }

        public override bool PreDraw(Projectile projectile, ref Color lightColor)
        {
            if (!isAlloyWandSegment ||
                projectile.ModProjectile is AlloyWandProjectile or AlloyWandProjectileTip)
            {
                return true;
            }

            string texturePath = projectile.type == ProjectileID.VilethornTip
                ? "everflow/Content/Projectiles/AlloyWandProjectile_2"
                : "everflow/Content/Projectiles/AlloyWandProjectile";

            Texture2D texture = ModContent.Request<Texture2D>(texturePath).Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = projectile.spriteDirection == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            Main.EntitySpriteDraw(
                texture,
                projectile.Center - Main.screenPosition,
                frame,
                projectile.GetAlpha(lightColor),
                projectile.rotation,
                origin,
                projectile.scale,
                effects);

            return false;
        }
    }
}
