using System;
using System.Collections.Generic;
using System.Reflection;
using everflow.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.Tile_Entities;
using Terraria.ModLoader;

namespace everflow.Common.Systems
{
    /// <summary>
    /// 武器架在场景绘制阶段直接读取物品的静态纹理，不会调用ModItem的物品栏绘制钩子。
    /// 因此在原版武器架绘制期间暂时隐藏汉堡王静态图标，随后用架内真实Item实例绘制馅料层。
    /// </summary>
    public sealed class BurgerKingWeaponRackSystem : ModSystem
    {
        private static MethodInfo drawSpecialTilesLegacyMethod;
        private static DrawSpecialTilesLegacyHook drawSpecialTilesLegacyHook;

        private delegate void DrawSpecialTilesLegacyOrig(
            TileDrawing self,
            Vector2 screenPosition,
            Vector2 offSet);

        private delegate void DrawSpecialTilesLegacyHook(
            DrawSpecialTilesLegacyOrig orig,
            TileDrawing self,
            Vector2 screenPosition,
            Vector2 offSet);

        public override void Load()
        {
            drawSpecialTilesLegacyMethod = typeof(TileDrawing).GetMethod(
                "DrawSpecialTilesLegacy",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new[] { typeof(Vector2), typeof(Vector2) },
                null);

            if (drawSpecialTilesLegacyMethod == null)
                throw new MissingMethodException(typeof(TileDrawing).FullName, "DrawSpecialTilesLegacy");

            drawSpecialTilesLegacyHook = DrawDynamicBurgersOnWeaponRacks;
            MonoModHooks.Add(drawSpecialTilesLegacyMethod, drawSpecialTilesLegacyHook);
        }

        public override void Unload()
        {
            drawSpecialTilesLegacyMethod = null;
            drawSpecialTilesLegacyHook = null;
        }

        private static void DrawDynamicBurgersOnWeaponRacks(
            DrawSpecialTilesLegacyOrig orig,
            TileDrawing self,
            Vector2 screenPosition,
            Vector2 offSet)
        {
            List<RackBurger> burgers = new();

            foreach (TileEntity entity in TileEntity.ByID.Values)
            {
                if (entity is TEWeaponsRack rack &&
                    rack.item != null &&
                    rack.item.ModItem is BurgerKing burger)
                {
                    burgers.Add(new RackBurger(rack.item, burger, rack.Position, rack.item.type));

                    // 原版会在这里直接画TextureAssets.Item[type]。临时置空类型，避免静态汉堡底图叠在动态图上。
                    rack.item.type = 0;
                }
            }

            try
            {
                orig(self, screenPosition, offSet);
            }
            finally
            {
                foreach (RackBurger entry in burgers)
                    entry.Item.type = entry.OriginalType;
            }

            foreach (RackBurger entry in burgers)
                DrawBurgerOnRack(entry, screenPosition, offSet);
        }

        private static void DrawBurgerOnRack(RackBurger entry, Vector2 screenPosition, Vector2 offSet)
        {
            Vector2 worldCenter = new Vector2(entry.Position.X * 16f + 24f, entry.Position.Y * 16f + 24f);
            Vector2 drawPosition = worldCenter - screenPosition + offSet;

            // 避免遍历世界中所有武器架时绘制屏幕外物品。
            if (drawPosition.X < -80f || drawPosition.Y < -80f ||
                drawPosition.X > Main.screenWidth + 80f || drawPosition.Y > Main.screenHeight + 80f)
            {
                return;
            }

            int[] fillings = entry.Burger.GetSelectedFillingFrames();
            int visualHeight = BurgerKing.FrameHeight + BurgerKing.LayerRise * (fillings.Length + 1);
            float visualSize = Math.Max(BurgerKing.FrameWidth, visualHeight);
            float scale = visualSize > 40f ? 40f / visualSize : 1f;

            Color lighting = Lighting.GetColor(entry.Position.X + 1, entry.Position.Y + 1);
            Color drawColor = entry.Item.GetAlpha(lighting);

            Tile tile = Main.tile[entry.Position.X, entry.Position.Y];
            SpriteEffects effects = tile.TileFrameX >= 54
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            BurgerKing.DrawBurger(
                Main.spriteBatch,
                drawPosition,
                drawColor,
                0f,
                new Vector2(BurgerKing.FrameWidth, BurgerKing.FrameHeight) * 0.5f,
                scale,
                effects,
                fillings);
        }

        private readonly record struct RackBurger(
            Item Item,
            BurgerKing Burger,
            Point16 Position,
            int OriginalType);
    }
}
