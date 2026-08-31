using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using everflow.Content.Items;

namespace everflow.Content.Systems
{
    public sealed class BurgerCookerUISystem : ModSystem
    {
        private const int PanelWidth = 720;
        private const int PanelHeight = 460;
        private const int CheckboxSize = 24;
        private const int RowHeight = 34;

        private static Item burgerSlot = new();
        private static Point openedTile;

        public static bool Visible { get; private set; }

        public override void Load()
        {
            burgerSlot.TurnToAir();
        }

        public override void Unload()
        {
            burgerSlot = new Item();
            Visible = false;
        }

        public static void Open(int i, int j)
        {
            if (Main.dedServ)
                return;

            if (Visible)
                Close(returnBurger: true);

            Tile tile = Framing.GetTileSafely(i, j);
            int localX = (tile.TileFrameX / 18) % 2;
            int localY = (tile.TileFrameY / 18) % 2;
            openedTile = new Point(i - localX, j - localY);
            Visible = true;
            Main.playerInventory = true;
            Main.LocalPlayer.chest = -1;
            Recipe.FindRecipes();
        }

        public static void Close(bool returnBurger)
        {
            if (!Visible && burgerSlot.IsAir)
                return;

            if (returnBurger && !burgerSlot.IsAir && Main.LocalPlayer.active)
            {
                Player player = Main.LocalPlayer;
                Item returning = burgerSlot.Clone();
                burgerSlot.TurnToAir();
                Item remainder = player.GetItem(
                    player.whoAmI,
                    returning,
                    GetItemSettings.InventoryEntityToPlayerInventorySettings);

                if (!remainder.IsAir)
                {
                    player.QuickSpawnItem(
                        player.GetSource_Misc("BurgerCookerUI"),
                        remainder,
                        remainder.stack);
                }
            }

            Visible = false;
        }

        public override void OnWorldUnload()
        {
            if (!Main.dedServ)
                Close(returnBurger: true);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (!Visible || Main.dedServ)
                return;

            Player player = Main.LocalPlayer;
            Vector2 cookerCenter = new Vector2(
                openedTile.X * 16f + 16f,
                openedTile.Y * 16f + 16f);

            if (!Main.playerInventory || player.dead ||
                Vector2.DistanceSquared(player.Center, cookerCenter) > 12f * 16f * 12f * 16f)
            {
                Close(returnBurger: true);
                return;
            }

            Rectangle panel = GetPanelRectangle();
            Point mouse = Main.MouseScreen.ToPoint();
            if (panel.Contains(mouse))
            {
                player.mouseInterface = true;
            }

        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (!Visible || Main.dedServ)
                return;

            Rectangle panel = GetPanelRectangle();
            DrawPanel(spriteBatch, panel);

            Utils.DrawBorderString(
                spriteBatch,
                Language.GetTextValue("Mods.everflow.UI.BurgerCooker.Title"),
                new Vector2(panel.X + 20, panel.Y + 14),
                new Color(255, 210, 135),
                1.05f);

            Utils.DrawBorderString(
                spriteBatch,
                Language.GetTextValue("Mods.everflow.UI.BurgerCooker.SlotLabel"),
                new Vector2(panel.X + 20, panel.Y + 48),
                Color.White,
                0.82f);

            Rectangle slotRectangle = GetSlotRectangle(panel);
            Vector2 slotPosition = new(slotRectangle.X, slotRectangle.Y);

            if (slotRectangle.Contains(Main.MouseScreen.ToPoint()))
            {
                Main.LocalPlayer.mouseInterface = true;

                bool mouseItemAllowed = Main.mouseItem.IsAir || Main.mouseItem.type == ModContent.ItemType<BurgerKing>();
                bool slotItemAllowed = burgerSlot.IsAir || burgerSlot.type == ModContent.ItemType<BurgerKing>();
                if (mouseItemAllowed && slotItemAllowed)
                    ItemSlot.Handle(ref burgerSlot, ItemSlot.Context.BankItem);
            }

            ItemSlot.Draw(
                spriteBatch,
                ref burgerSlot,
                ItemSlot.Context.BankItem,
                slotPosition,
                Color.White);

            Point mouse = Main.MouseScreen.ToPoint();
            if (slotRectangle.Contains(mouse))
                ItemSlot.MouseHover(ref burgerSlot, ItemSlot.Context.BankItem);

            if (burgerSlot.ModItem is not BurgerKing burger)
            {
                Utils.DrawBorderString(
                    spriteBatch,
                    Language.GetTextValue("Mods.everflow.UI.BurgerCooker.InsertPrompt"),
                    new Vector2(panel.X + 105, panel.Y + 82),
                    Color.Silver,
                    0.85f);
                return;
            }

            Utils.DrawBorderString(
                spriteBatch,
                Language.GetTextValue(
                    "Mods.everflow.UI.BurgerCooker.SelectedCount",
                    burger.FillingCount,
                    BurgerKing.MaximumFillingLayers),
                new Vector2(panel.X + 105, panel.Y + 78),
                burger.FillingCount >= BurgerKing.MaximumFillingLayers
                    ? new Color(255, 190, 90)
                    : Color.White,
                0.85f);

            int displayedIndex = 0;
            Texture2D burgerTexture = ModContent.Request<Texture2D>(
                "everflow/Content/Items/BurgerKing").Value;
            foreach (BurgerKingFillingDefinition filling in BurgerKingFillingCatalog.HiddenCandidates)
            {
                Rectangle checkbox = GetCheckboxRectangle(panel, displayedIndex++);
                Rectangle fillingRow = new Rectangle(checkbox.X, checkbox.Y, 285, CheckboxSize);
                bool unlocked = BurgerKingFillingCatalog.IsUnlocked(filling.UnlockCondition);
                bool selected = burger.HasFilling(filling.FrameIndex);

                if (unlocked && fillingRow.Contains(mouse) && Main.mouseLeft && Main.mouseLeftRelease)
                {
                    burger.ToggleFilling(filling.FrameIndex);
                    Main.mouseLeftRelease = false;
                    selected = burger.HasFilling(filling.FrameIndex);
                }

                Texture2D checkboxTexture = selected
                    ? TextureAssets.InventoryTickOn.Value
                    : TextureAssets.InventoryTickOff.Value;
                Color entryColor = unlocked ? Color.White : Color.Gray * 0.55f;
                spriteBatch.Draw(checkboxTexture, checkbox, entryColor);

                Rectangle fillingSource = new Rectangle(
                    0,
                    filling.FrameIndex * BurgerKing.FrameStride,
                    BurgerKing.FrameWidth,
                    BurgerKing.FrameHeight);
                Rectangle fillingDestination = new Rectangle(
                    checkbox.Right + 5,
                    checkbox.Y + 2,
                    38,
                    21);
                spriteBatch.Draw(
                    burgerTexture,
                    fillingDestination,
                    fillingSource,
                    entryColor);

                Color nameColor = !unlocked
                    ? Color.Gray
                    : selected
                    ? new Color(255, 225, 145)
                    : Color.White;
                Utils.DrawBorderString(
                    spriteBatch,
                    filling.ChineseName,
                    new Vector2(fillingDestination.Right + 5, checkbox.Y - 1),
                    nameColor,
                    0.68f);
                Utils.DrawBorderString(
                    spriteBatch,
                    GetCategoryText(filling.Categories),
                    new Vector2(fillingDestination.Right + 5, checkbox.Y + 15),
                    unlocked ? new Color(225, 184, 90) : Color.Gray,
                    0.52f);

                if (fillingRow.Contains(mouse))
                {
                    Main.LocalPlayer.mouseInterface = true;
                    Main.instance.MouseText(BuildFillingTooltip(filling, unlocked));
                }
            }
        }

        private static Rectangle GetPanelRectangle()
        {
            int x = System.Math.Max(10, (Main.screenWidth - PanelWidth) / 2);
            int y = System.Math.Max(50, (Main.screenHeight - PanelHeight) / 2);
            return new Rectangle(x, y, PanelWidth, PanelHeight);
        }

        private static Rectangle GetSlotRectangle(Rectangle panel) =>
            new(panel.X + 24, panel.Y + 76, 52, 52);

        private static Rectangle GetCheckboxRectangle(Rectangle panel, int displayedIndex)
        {
            int column = displayedIndex / 10;
            int row = displayedIndex % 10;
            return new Rectangle(
                panel.X + 105 + column * 300,
                panel.Y + 112 + row * RowHeight,
                CheckboxSize,
                CheckboxSize);
        }

        private static void DrawPanel(SpriteBatch spriteBatch, Rectangle panel)
        {
            Utils.DrawSplicedPanel(
                spriteBatch,
                TextureAssets.InventoryBack.Value,
                panel.X,
                panel.Y,
                panel.Width,
                panel.Height,
                12,
                12,
                12,
                12,
                Color.White * 0.96f);
        }

        private static string BuildFillingTooltip(BurgerKingFillingDefinition filling, bool unlocked)
        {
            string result = filling.ChineseName;
            if (!unlocked)
                result += $"\n[未解锁] {GetUnlockConditionText(filling.UnlockCondition)}";
            if (filling.DamageDelta != 0)
                result += $"\n伤害 {(filling.DamageDelta > 0 ? "+" : string.Empty)}{filling.DamageDelta}";
            if (filling.UseTimePercentDelta != 0)
                result += $"\n使用时间 {filling.UseTimePercentDelta}%";
            if (filling.KnockbackDelta != 0f)
                result += $"\n击退 +{filling.KnockbackDelta:0.#}";
            if (filling.CritDelta != 0)
                result += $"\n暴击 +{filling.CritDelta}%";
            if (filling.ProjectileCountDelta != 0)
                result += $"\n发射数量 +{filling.ProjectileCountDelta}";
            if (filling.FrameIndex == 18)
                result += "\n三枚汉堡呈扇形散射";
            if (filling.PenetrationDelta != 0)
                result += $"\n穿透敌人数 +{filling.PenetrationDelta}";
            if (filling.ExplosionSize > 0)
                result += $"\n爆炸范围 {filling.ExplosionSize}×{filling.ExplosionSize}";
            if (filling.Debuff != BurgerKingFillingDebuff.None)
                result += $"\n命中施加：{filling.Debuff}";
            result += $"\n右键食用：{GetEatingEffectText(filling.FrameIndex)}";
            return result;
        }

        private static string GetUnlockConditionText(BurgerKingUnlockCondition condition) => condition switch
        {
            BurgerKingUnlockCondition.SlimeKing => "击败史莱姆王后解锁",
            BurgerKingUnlockCondition.EyeOfCthulhu => "击败克苏鲁之眼后解锁",
            BurgerKingUnlockCondition.EaterOfWorlds => "击败世界吞噬怪后解锁",
            BurgerKingUnlockCondition.BrainOfCthulhu => "击败克苏鲁之脑后解锁",
            BurgerKingUnlockCondition.QueenBee => "击败蜂王后解锁",
            BurgerKingUnlockCondition.Deerclops => "击败独眼巨鹿后解锁",
            BurgerKingUnlockCondition.Skeletron => "击败骷髅王后解锁",
            BurgerKingUnlockCondition.WallOfFlesh => "击败血肉墙后解锁",
            BurgerKingUnlockCondition.QueenSlime => "击败史莱姆皇后后解锁",
            BurgerKingUnlockCondition.Spazmatism => "击败魔焰眼后解锁",
            BurgerKingUnlockCondition.Retinazer => "击败激光眼后解锁",
            BurgerKingUnlockCondition.Destroyer => "击败毁灭者后解锁",
            BurgerKingUnlockCondition.SkeletronPrime => "击败机械骷髅王后解锁",
            BurgerKingUnlockCondition.Plantera => "击败世纪之花后解锁",
            BurgerKingUnlockCondition.Golem => "击败石巨人后解锁",
            BurgerKingUnlockCondition.DukeFishron => "击败猪龙鱼公爵后解锁",
            BurgerKingUnlockCondition.EmpressOfLight => "击败光之女皇后解锁",
            BurgerKingUnlockCondition.LunaticCultist => "击败拜月教邪教徒后解锁",
            BurgerKingUnlockCondition.MoonLord => "击败月亮领主后解锁",
            _ => "尚未满足解锁条件"
        };

        private static string GetEatingEffectText(int frameIndex) => frameIndex switch
        {
            2 => "回复15生命值",
            3 => "防御力+8",
            4 => "免疫中毒与石化",
            5 => "移动速度提高10%",
            6 => "持续回复生命值",
            7 => "食用Buff持续时间提高50%",
            8 => "所有属性小幅提高",
            9 => "回复50生命值",
            10 => "获得一个随机增益",
            11 => "免疫减速与混乱",
            12 => "免疫冰冻与寒冷",
            13 => "免疫黑暗与诅咒",
            14 => "所有属性中幅提高",
            15 => "攻击速度提高20%",
            16 => "免疫岩浆与燃烧",
            17 => "获得水下呼吸",
            18 => "获得带电与灵液",
            19 => "移动速度提高50%",
            20 => "回复100生命值",
            _ => "无"
        };

        private static string GetCategoryText(BurgerKingFillingCategory categories)
        {
            List<string> names = new();
            if ((categories & BurgerKingFillingCategory.Classic) != 0)
                names.Add("经典");
            if ((categories & BurgerKingFillingCategory.DarkCuisine) != 0)
                names.Add("黑暗料理");
            if ((categories & BurgerKingFillingCategory.Vegetarian) != 0)
                names.Add("素食主义");
            if ((categories & BurgerKingFillingCategory.Carnivore) != 0)
                names.Add("肉食主义");
            if ((categories & BurgerKingFillingCategory.Condiment) != 0)
                names.Add("调味料");
            return string.Join(" / ", names);
        }
    }
}
