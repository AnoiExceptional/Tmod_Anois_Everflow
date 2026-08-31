using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using everflow.Content.Systems;

namespace everflow.Content.Tiles
{
    public sealed class BurgerCooker : ModTile
    {
        public override string Texture => "everflow/Content/Tiles/BurgerCooker_Tile";

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileLavaDeath[Type] = true;

            DustType = DustID.Iron;
            HitSound = SoundID.Tink;
            AdjTiles = new int[] { TileID.CookingPots };

            AddMapEntry(
                new Color(132, 90, 60),
                Language.GetText("Mods.everflow.Items.BurgerCooker.DisplayName"));
            RegisterItemDrop(ModContent.ItemType<Items.BurgerCooker>());

            TileObjectData.newTile.CopyFrom(
                TileObjectData.GetTileData(TileID.CookingPots, 0));
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.StyleWrapLimit = 1;
            TileObjectData.addTile(Type);
        }

        public override bool RightClick(int i, int j)
        {
            BurgerCookerUISystem.Open(i, j);
            return true;
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<Items.BurgerCooker>();
        }
    }
}
