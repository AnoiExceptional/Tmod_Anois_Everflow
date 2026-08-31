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
            RegisterItemDrop(ModContent.ItemType<AlloyBar>());

            // 完整继承原版金属锭的1x1放置规则；物品的placeStyle固定为0，
            // 因此始终读取EverflowBars_Placed.png的第一个18x18样式。
            TileObjectData.newTile.CopyFrom(
                TileObjectData.GetTileData(TileID.MetalBars, 0));
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleWrapLimit = 1;
            TileObjectData.addTile(Type);
        }
    }
}
