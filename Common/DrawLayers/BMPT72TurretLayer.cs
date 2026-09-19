using everflow.Content.Mounts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace everflow.Common.DrawLayers
{
    public sealed class BMPT72TurretLayer : PlayerDrawLayer
    {
        public override Position GetDefaultPosition() =>
            new BeforeParent(PlayerDrawLayers.MountFront);

        protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            Player player = drawInfo.drawPlayer;
            if (!player.mount.Active || player.mount.Type != ModContent.MountType<BMPT72>())
                return;

            Texture2D texture = ModContent.Request<Texture2D>(
                "everflow/Content/Mounts/BMPT_72_Turret").Value;
            Vector2 feetWorld = BMPT72.GetVisualFeetWorld(player);
            SpriteEffects effects = player.direction < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            DrawData data = new(
                texture,
                BMPT72.RoundScreenPosition(feetWorld),
                null,
                Lighting.GetColor(feetWorld.ToTileCoordinates()),
                0f,
                new Vector2(BMPT72.HullMirrorAxisX, BMPT72.GroundY),
                1f,
                effects,
                0f);
            data.shader = player.cMount;
            drawInfo.DrawDataCache.Add(data);
        }
    }
}
