using everflow.Content.Mounts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace everflow.Common.DrawLayers
{
    /// <summary>将火箭发射架绘制在原版MountFront车体的上层。</summary>
    public sealed class BMPT72RocketWeaponLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() =>
            new AfterParent(PlayerDrawLayers.MountFront);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (!player.mount.Active || player.mount.Type != ModContent.MountType<BMPT72>())
                return;

            Texture2D texture = ModContent.Request<Texture2D>(
                "everflow/Content/Mounts/BMPT_72_Weapon_2").Value;
            Vector2 pivotWorld = BMPT72.GetRocketWeaponPivotWorld(player);
            Vector2 aim = Main.MouseWorld - pivotWorld;
            if (aim.LengthSquared() < 0.001f)
                aim = Vector2.UnitX * player.direction;

            bool facingLeft = aim.X < 0f;
            Vector2 origin = facingLeft
                ? new Vector2(
                    BMPT72.RocketWeaponPivotInFrame.X,
                    BMPT72.FrameHeight - BMPT72.RocketWeaponPivotInFrame.Y)
                : BMPT72.RocketWeaponPivotInFrame;

            DrawData data = new(
                texture,
                BMPT72.RoundScreenPosition(pivotWorld),
                null,
                Lighting.GetColor(pivotWorld.ToTileCoordinates()),
                aim.ToRotation(),
                origin,
                1f,
                facingLeft ? SpriteEffects.FlipVertically : SpriteEffects.None,
                0f);
            data.shader = player.cMount;
            drawInfo.DrawDataCache.Add(data);
        }
    }
}
