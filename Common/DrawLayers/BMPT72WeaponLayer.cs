using everflow.Content.Mounts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace everflow.Common.DrawLayers
{
    public sealed class BMPT72WeaponLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() =>
            new BeforeParent(ModContent.GetInstance<BMPT72TurretLayer>());

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (!player.mount.Active || player.mount.Type != ModContent.MountType<BMPT72>())
                return;

            Texture2D texture = ModContent.Request<Texture2D>(
                "everflow/Content/Mounts/BMPT_72_Weapon_1").Value;
            Vector2 pivotWorld = BMPT72.GetWeaponPivotWorld(player);
            Vector2 aim = Main.MouseWorld - pivotWorld;
            if (aim.LengthSquared() < 0.001f)
                aim = Vector2.UnitX * player.direction;

            bool facingLeft = aim.X < 0f;
            Vector2 drawOrigin = WeaponDrawOrigin(facingLeft);

            DrawData data = new(
                texture,
                BMPT72.RoundScreenPosition(pivotWorld),
                null,
                Lighting.GetColor(pivotWorld.ToTileCoordinates()),
                aim.ToRotation(),
                drawOrigin,
                1f,
                facingLeft ? SpriteEffects.FlipVertically : SpriteEffects.None,
                0f);
            data.shader = player.cMount;
            drawInfo.DrawDataCache.Add(data);
        }

        private static Vector2 WeaponDrawOrigin(bool facingLeft)
        {
            if (!facingLeft)
                return BMPT72.WeaponPivotInFrame;

            // FlipVertically会同时翻转源纹理中的原点位置。使用镜像后的
            // Y坐标，保证源图(103,42)处的轴心仍然落在world pivot上。
            return new Vector2(
                BMPT72.WeaponPivotInFrame.X,
                BMPT72.FrameHeight - BMPT72.WeaponPivotInFrame.Y);
        }
    }
}
