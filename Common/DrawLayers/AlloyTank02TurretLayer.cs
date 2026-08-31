using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using everflow.Content.Mounts;

namespace everflow.Common.DrawLayers
{
    /// <summary>
    /// Draws above the driver but immediately below the hull front layer.
    /// </summary>
    public sealed class AlloyTank02TurretLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() =>
            new BeforeParent(PlayerDrawLayers.MountFront);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (!player.mount.Active ||
                player.mount.Type != ModContent.MountType<AlloyTank02>())
            {
                return;
            }

            Texture2D texture = ModContent.Request<Texture2D>(
                "everflow/Content/Mounts/AlloyTank02_Turret").Value;

            // The collision center is also the turret's mirror axis. The
            // texture flips around its own centered origin without changing
            // its world position.
            Vector2 turretCenter = AlloyTank02.GetTurretCenterWorld(player);

            SpriteEffects effects = player.direction < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            DrawData turret = new(
                texture,
                AlloyTank02.RoundScreenPosition(turretCenter),
                null,
                Lighting.GetColor(turretCenter.ToTileCoordinates()),
                0f,
                texture.Size() * 0.5f,
                AlloyTank02.TankDrawScale,
                effects,
                0f);
            // 自定义分离图层不会自动继承坐骑栏染料，显式使用与车体相同的着色器。
            turret.shader = player.cMount;

            drawInfo.DrawDataCache.Add(turret);
        }
    }
}
