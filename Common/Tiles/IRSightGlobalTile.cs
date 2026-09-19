using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Common.Tiles
{
    public sealed class IRSightGlobalTile : GlobalTile
    {
        public override void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b)
        {
            if (Main.dedServ || Main.gameMenu || Main.LocalPlayer == null ||
                !Main.LocalPlayer.HasBuff(ModContent.BuffType<IRSightBuff>()))
                return;

            // 红外模式下，所有物块光源都视为冷光并禁止写入环境光照。
            // 这同时覆盖原版与Mod金属锭、宝石火花、荧光物块及其他发光物块。
            // 岩浆是液体而非物块，由IRSightVisualSystem另行注入真实热照明。
            r = 0f;
            g = 0f;
            b = 0f;
        }

        public override void DrawEffects(int i, int j, int type, SpriteBatch spriteBatch, ref TileDrawInfo drawData)
        {
            if (!InfraredIsActive())
                return;

            if (type == TileID.LihzahrdBrick)
            {
                // 神庙砖导热极快，不保留可供热成像辨认的温差。
                drawData.tileLight = Color.Black;
                return;
            }

            if (TileID.Sets.Torch[type] || TileID.Sets.Campfire[type])
            {
                // 火把和篝火：强高亮，但不照亮周围。
                drawData.tileLight = new Color(220, 220, 220);
            }
            else if (TileID.Sets.TouchDamageHot[type])
            {
                // 会造成燃烧的物块：仅比普通物块更亮。
                drawData.tileLight = new Color(112, 112, 112);
            }
            else
            {
                // 普通物块是“弱弱热源”：不给环境投光，但保证自身有最低
                // 可见亮度。该下限低于火块，不会把洞穴地形照成亮灰色。
                drawData.tileLight = RaiseGrayFloor(drawData.tileLight, 42);
            }
        }

        private static Color RaiseGrayFloor(Color source, byte floor)
        {
            byte r = source.R < floor ? floor : source.R;
            byte g = source.G < floor ? floor : source.G;
            byte b = source.B < floor ? floor : source.B;
            return new Color(r, g, b, source.A);
        }

        private static bool InfraredIsActive() =>
            !Main.dedServ && !Main.gameMenu && Main.LocalPlayer != null &&
            Main.LocalPlayer.HasBuff(ModContent.BuffType<IRSightBuff>());
    }
}
