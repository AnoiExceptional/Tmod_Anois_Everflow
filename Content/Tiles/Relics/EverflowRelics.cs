using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace everflow.Content.Tiles.Relics
{
    public abstract class EverflowRelicTileBase : ModTile
    {
        private const int RelicFrameSize = 50;

        public override string Texture =>
            "everflow/Content/Tiles/Relics/EverflowRelicTile";

        protected abstract int RelicFrame { get; }
        protected abstract string ItemLocalizationKey { get; }

        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x4);
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.addTile(Type);

            AddMapEntry(
                new Color(233, 207, 94),
                Language.GetText(ItemLocalizationKey));
            DustType = -1;
        }

        public override void DrawEffects(
            int i,
            int j,
            SpriteBatch spriteBatch,
            ref TileDrawInfo drawData)
        {
            Tile tile = Main.tile[i, j];
            if (tile.TileFrameX == 0 && tile.TileFrameY == 0)
            {
                Main.instance.TilesRenderer.AddSpecialPoint(
                    i,
                    j,
                    TileDrawing.TileCounterType.CustomNonSolid);
            }
        }

        public override void SpecialDraw(int i, int j, SpriteBatch spriteBatch)
        {
            Texture2D tops = ModContent.Request<Texture2D>(
                "everflow/Content/Tiles/Relics/EverflowRelicTops").Value;
            Rectangle source = new Rectangle(
                0,
                RelicFrame * RelicFrameSize,
                RelicFrameSize,
                RelicFrameSize);
            Vector2 origin = source.Size() * 0.5f;

            float wave = (float)System.Math.Sin(
                Main.GlobalTimeWrappedHourly * MathHelper.TwoPi / 5f);
            Vector2 worldPosition = new Point(i, j).ToWorldCoordinates(24f, 64f) +
                new Vector2(0f, -40f + wave * 4f);
            Vector2 drawPosition = worldPosition - Main.screenPosition;
            Color lightColor = Lighting.GetColor(i, j);

            spriteBatch.Draw(
                tops,
                drawPosition,
                source,
                lightColor,
                0f,
                origin,
                1f,
                SpriteEffects.None,
                0f);

            float pulse = (float)System.Math.Sin(
                Main.GlobalTimeWrappedHourly * MathHelper.TwoPi / 2f) * 0.3f + 0.7f;
            Color glowColor = lightColor;
            glowColor.A = 0;
            glowColor *= 0.1f * pulse;
            float glowRadius = 6f + wave * 2f;

            for (int k = 0; k < 6; k++)
            {
                Vector2 offset = (MathHelper.TwoPi * k / 6f)
                    .ToRotationVector2() * glowRadius;
                spriteBatch.Draw(
                    tops,
                    drawPosition + offset,
                    source,
                    glowColor,
                    0f,
                    origin,
                    1f,
                    SpriteEffects.None,
                    0f);
            }
        }
    }

    public sealed class AncientTroopRelicTile : EverflowRelicTileBase
    {
        protected override int RelicFrame => 0;
        protected override string ItemLocalizationKey =>
            "Mods.everflow.Items.AncientTroopRelic.DisplayName";
    }

    public sealed class YaZiRelicTile : EverflowRelicTileBase
    {
        protected override int RelicFrame => 1;
        protected override string ItemLocalizationKey =>
            "Mods.everflow.Items.YaZiRelic.DisplayName";
    }
}
