using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using everflow.Content.Items.Materials;

namespace everflow.Content.Tiles
{
    public class AlloyBarPlaced : ModTile
    {
        private const int FrameWidth = 18;
        private const int WyrmBladeBarStyle = 1;
        private static readonly Color WyrmBladeBarColor = new Color(140, 191, 199);

        public override string Texture =>
            "everflow/Content/Tiles/EverflowBars_Placed";

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = Main.tileSolid[TileID.MetalBars];
            Main.tileSolidTop[Type] = Main.tileSolidTop[TileID.MetalBars];
            Main.tileNoAttach[Type] = Main.tileNoAttach[TileID.MetalBars];
            Main.tileLavaDeath[Type] = Main.tileLavaDeath[TileID.MetalBars];
            Main.tileShine[Type] = Main.tileShine[TileID.MetalBars];
            Main.tileShine2[Type] = Main.tileShine2[TileID.MetalBars];

            DustType = DustID.Iron;
            HitSound = SoundID.Tink;

            AddMapEntry(
                new Color(160, 170, 180),
                Language.GetText("Mods.everflow.Items.AlloyBar.DisplayName"));
            AddMapEntry(
                WyrmBladeBarColor,
                Language.GetText("Mods.everflow.Items.WyrmBladeBar.DisplayName"));
            RegisterItemDrop(ModContent.ItemType<AlloyBar>(), 0);
            RegisterItemDrop(
                ModContent.ItemType<WyrmBladeBar>(),
                WyrmBladeBarStyle);

            // 完整继承原版金属锭的1x1放置规则。共享贴图按横向18px
            // 分帧：合金锭使用第1帧，虫脊锭使用其右侧的第2帧。
            TileObjectData.newTile.CopyFrom(
                TileObjectData.GetTileData(TileID.MetalBars, 0));
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleWrapLimit = 23;
            TileObjectData.addTile(Type);
        }

        public override ushort GetMapOption(int i, int j)
        {
            int style = Main.tile[i, j].TileFrameX / FrameWidth;
            return (ushort)(style == WyrmBladeBarStyle ? 1 : 0);
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            int style = Main.tile[i, j].TileFrameX / FrameWidth;
            if (style != WyrmBladeBarStyle)
                return true;

            Dust.NewDust(
                new Vector2(i * 16f, j * 16f),
                16,
                16,
                DustID.TintableDustLighted,
                newColor: WyrmBladeBarColor);
            return false;
        }
    }
}
