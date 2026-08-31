using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public class AlloyPistolFlame : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool FromAlloyPistol;

        // 只覆盖 AlloyPistol 生成的原版火焰，其他喷火武器不受影响。
        public override bool? CanDamage(Projectile projectile) =>
            FromAlloyPistol ? true : null;

        public override void ModifyDamageHitbox(
            Projectile projectile,
            ref Microsoft.Xna.Framework.Rectangle hitbox)
        {
            if (!FromAlloyPistol)
                return;

            const int minimumSize = 24;

            if (hitbox.Width < minimumSize || hitbox.Height < minimumSize)
            {
                Microsoft.Xna.Framework.Point center = hitbox.Center;
                hitbox = new Microsoft.Xna.Framework.Rectangle(
                    center.X - minimumSize / 2,
                    center.Y - minimumSize / 2,
                    minimumSize,
                    minimumSize);
            }
        }
    }
}
