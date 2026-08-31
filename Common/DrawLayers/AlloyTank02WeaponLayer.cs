using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using everflow.Content.Mounts;

namespace everflow.Common.DrawLayers
{
    public class AlloyTank02WeaponLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() =>
            new BeforeParent(ModContent.GetInstance<AlloyTank02TurretLayer>());

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (!player.mount.Active ||
                player.mount.Type != ModContent.MountType<AlloyTank02>())
            {
                return;
            }

            Texture2D texture = ModContent.Request<Texture2D>(
                "everflow/Content/Mounts/AlloyTank02_Weapon").Value;

            // Share the fixed turret center. Rotation alone points the barrel
            // left or right; translating the pivot by player.direction made
            // the whole assembly jump across the driver.
            Vector2 pivotWorld = AlloyTank02.GetWeaponPivotWorld(player);

            Vector2 aimDirection = Main.MouseWorld - pivotWorld;
            if (aimDirection.LengthSquared() < 0.001f)
                aimDirection = Vector2.UnitX * player.direction;

            SpriteEffects weaponEffects = aimDirection.X < 0f
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None;

            DrawData weapon = new(
                texture,
                AlloyTank02.RoundScreenPosition(pivotWorld),
                null,
                Lighting.GetColor(pivotWorld.ToTileCoordinates()),
                aimDirection.ToRotation(),
                // Keep the rotation axis on the common vehicle centerline,
                // but begin the visible barrel one tile farther forward.
                new Vector2(
                    -AlloyTank02.WeaponForwardOffset / AlloyTank02.WeaponDrawScale,
                    texture.Height * 0.5f),
                AlloyTank02.WeaponDrawScale,
                weaponEffects,
                0f);
            // 炮管与原版MountFront车体共用坐骑染料槽。
            weapon.shader = player.cMount;

            drawInfo.DrawDataCache.Add(weapon);
        }
    }
}
