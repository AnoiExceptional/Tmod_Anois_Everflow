using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using everflow.Common;
using everflow.Content.Buffs;
using everflow.Content.Players;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    public sealed class BurgerKing : ModItem
    {
        private int filling1;
        private int filling2;
        private int filling3;
        private int filling4;
        private int filling5;

        public const int FrameWidth = 48;
        public const int FrameHeight = 26;
        public const int FrameStride = 28;
        public const int LayerRise = 4;
        public const int EmptyBurgerHeight = FrameHeight + LayerRise;
        public const int MaximumFillingLayers = 5;

        // Interfaces such as Item Browser bypass PreDrawInInventory and display
        // the registered item texture directly, so give them a correctly-sized
        // empty-burger fallback. Dynamic drawing still reads BurgerKing.png.
        public override string Texture => "everflow/Content/Items/BurgerKing_Icon";

        public override void SetDefaults()
        {
            Item.width = FrameWidth;
            Item.height = EmptyBurgerHeight;
            Item.damage = 15;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 5f;
            Item.crit = 4;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<BurgerKingProjectile>();
            Item.shootSpeed = 20f;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.White;
            ApplyFillingStats();
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool AllowPrefix(int pre) => false;

        public override bool? PrefixChance(int pre, UnifiedRandom rand) => false;

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int classic = 0;
            int condiment = 0;
            int vegetarian = 0;
            int carnivore = 0;
            int darkCuisine = 0;

            foreach (int frame in GetSelectedFillingFrames())
            {
                if (!BurgerKingFillingCatalog.TryGet(frame, out BurgerKingFillingDefinition filling))
                    continue;

                BurgerKingFillingCategory categories = filling.Categories;
                if ((categories & BurgerKingFillingCategory.Classic) != 0) classic++;
                if ((categories & BurgerKingFillingCategory.Condiment) != 0) condiment++;
                if ((categories & BurgerKingFillingCategory.Vegetarian) != 0) vegetarian++;
                if ((categories & BurgerKingFillingCategory.Carnivore) != 0) carnivore++;
                if ((categories & BurgerKingFillingCategory.DarkCuisine) != 0) darkCuisine++;
            }

            Color gold = new Color(255, 205, 80);
            if (classic >= 3)
                AddCombinationTooltip(tooltips, "ClassicCombination", "经典组合：所有属性大幅提高", gold);
            if (condiment >= 3)
                AddCombinationTooltip(tooltips, "CondimentCombination", "调味料组合：免疫所有持续掉血Debuff", gold);
            if (vegetarian >= 3)
                AddCombinationTooltip(tooltips, "VegetarianCombination", "素食主义组合：攻击速度提高20%", gold);
            if (carnivore >= 3)
                AddCombinationTooltip(tooltips, "CarnivoreCombination", "肉食主义组合：暴击率提高20%", gold);
            if (darkCuisine >= 3)
                AddCombinationTooltip(tooltips, "DarkCuisineCombination", "黑暗料理组合：获得1%吸血", gold);

            tooltips.Add(new TooltipLine(
                Mod,
                "MoreYouEat",
                Language.GetTextValue("Mods.everflow.Items.BurgerKing.MoreYouEat"))
            {
                OverrideColor = new Color(90, 220, 100)
            });
        }

        private void AddCombinationTooltip(List<TooltipLine> tooltips, string name, string text, Color color)
        {
            tooltips.Add(new TooltipLine(Mod, name, text) { OverrideColor = color });
        }

        public override bool CanUseItem(Player player)
        {
            int eatingVisualType = ModContent.ProjectileType<BurgerKingEatingVisual>();

            if (player.altFunctionUse == 2)
            {
                if (player.HasBuff(ModContent.BuffType<BurgerKingEatingCooldown>()))
                    return false;

                ConfigureEatingUse();
            }
            else
            {
                // Do not let a new throwing animation replace the active eating
                // animation. The visual projectile also uses this interval to
                // determine when the burger has reached the player's mouth.
                if (player.ownedProjectileCounts[eatingVisualType] > 0)
                    return false;

                ConfigureThrowingUse();
            }

            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse != 2 ||
                player.itemAnimation != player.itemAnimationMax)
            {
                return null;
            }

            int[] fillings = GetSelectedFillingFrames();
            int duration = HasFilling(7) ? 900 : 600;
            player.GetModPlayer<BurgerKingMealPlayer>().ConsumeBurger(fillings, duration);
            player.AddBuff(ModContent.BuffType<BurgerKingMealBuff>(), duration);
            player.AddBuff(ModContent.BuffType<BurgerKingEatingCooldown>(), 1800);

            if (player.whoAmI == Main.myPlayer)
            {
                int visualType = ModContent.ProjectileType<BurgerKingEatingVisual>();
                if (player.ownedProjectileCounts[visualType] <= 0)
                {
                    Projectile.NewProjectile(
                        player.GetSource_ItemUse(Item),
                        player.MountedCenter,
                        Vector2.Zero,
                        visualType,
                        0,
                        0f,
                        player.whoAmI);
                }
            }

            return true;
        }

        public override void HoldItem(Player player)
        {
            if (player.itemAnimation <= 0)
                ConfigureThrowingUse();
        }

        public override void UpdateInventory(Player player)
        {
            if (!ReferenceEquals(player.HeldItem, Item) || player.itemAnimation <= 0)
                ApplyFillingStats();
        }

        private void ConfigureThrowingUse()
        {
            Item.useStyle = ItemUseStyleID.Swing;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<BurgerKingProjectile>();
            Item.shootSpeed = 20f;
            ApplyFillingStats();
        }

        private void ConfigureEatingUse()
        {
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.autoReuse = false;
            Item.UseSound = SoundID.Item2;
            Item.shoot = ProjectileID.None;
            Item.shootSpeed = 0f;
        }

        public override bool PreDrawInInventory(
            SpriteBatch spriteBatch,
            Vector2 position,
            Rectangle frame,
            Color drawColor,
            Color itemColor,
            Vector2 origin,
            float scale)
        {
            DrawBurger(
                spriteBatch,
                position,
                drawColor,
                0f,
                new Vector2(FrameWidth, FrameHeight) * 0.5f,
                scale,
                SpriteEffects.None,
                GetSelectedFillingFrames());
            return false;
        }

        public override bool PreDrawInWorld(
            SpriteBatch spriteBatch,
            Color lightColor,
            Color alphaColor,
            ref float rotation,
            ref float scale,
            int whoAmI)
        {
            DrawBurger(
                spriteBatch,
                Item.Center - Main.screenPosition,
                lightColor,
                rotation,
                new Vector2(FrameWidth, FrameHeight) * 0.5f,
                scale,
                SpriteEffects.None,
                GetSelectedFillingFrames());
            return false;
        }

        public override bool Shoot(
            Player player,
            EntitySource_ItemUse_WithAmmo source,
            Vector2 position,
            Vector2 velocity,
            int type,
            int damage,
            float knockback)
        {
            if (player.altFunctionUse == 2)
                return false;

            FillingStats stats = CalculateFillingStats();
            int[] fillings = GetSelectedFillingFrames();
            bool prismaticTripleFan = HasFilling(18) && stats.ProjectileCount == 3;
            for (int i = 0; i < stats.ProjectileCount; i++)
            {
                float spread = prismaticTripleFan
                    ? MathHelper.Lerp(-0.08f, 0.08f, i / 2f)
                    : 0f;
                int projectileDamage = prismaticTripleFan && i != 1
                    ? System.Math.Max(1, (int)System.Math.Round(damage * 0.5f))
                    : damage;

                int projectileIndex = Projectile.NewProjectile(
                    source,
                    position,
                    velocity.RotatedBy(spread),
                    ModContent.ProjectileType<BurgerKingProjectile>(),
                    projectileDamage,
                    knockback,
                    player.whoAmI);

                if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles &&
                    Main.projectile[projectileIndex].ModProjectile is BurgerKingProjectile burger)
                {
                    burger.Configure(fillings, stats.PenetrationBonus, stats.ExplosionSize);
                    Main.projectile[projectileIndex].netUpdate = true;
                }
            }

            return false;
        }

        internal int[] GetSelectedFillingFrames()
        {
            List<int> frames = new(MaximumFillingLayers);
            AddIfPresent(frames, filling1);
            AddIfPresent(frames, filling2);
            AddIfPresent(frames, filling3);
            AddIfPresent(frames, filling4);
            AddIfPresent(frames, filling5);
            return frames.ToArray();
        }

        internal bool HasFilling(int frameIndex) =>
            filling1 == frameIndex || filling2 == frameIndex ||
            filling3 == frameIndex || filling4 == frameIndex ||
            filling5 == frameIndex;

        internal int FillingCount =>
            (filling1 > 0 ? 1 : 0) +
            (filling2 > 0 ? 1 : 0) +
            (filling3 > 0 ? 1 : 0) +
            (filling4 > 0 ? 1 : 0) +
            (filling5 > 0 ? 1 : 0);

        internal bool ToggleFilling(int frameIndex)
        {
            if (!BurgerKingFillingCatalog.TryGet(frameIndex, out BurgerKingFillingDefinition definition) ||
                !BurgerKingFillingCatalog.IsUnlocked(definition.UnlockCondition))
            {
                return false;
            }

            if (HasFilling(frameIndex))
            {
                RemoveFilling(frameIndex);
                ApplyFillingStats();
                return true;
            }

            if (FillingCount >= MaximumFillingLayers)
                return false;

            if (filling1 == 0) filling1 = frameIndex;
            else if (filling2 == 0) filling2 = frameIndex;
            else if (filling3 == 0) filling3 = frameIndex;
            else if (filling4 == 0) filling4 = frameIndex;
            else filling5 = frameIndex;

            ApplyFillingStats();
            return true;
        }

        internal void ApplyFillingStats()
        {
            // Burger King recalculates its panel from the selected fillings.
            // Remove prefixes before that calculation so old prefixed instances
            // are repaired instead of repeatedly multiplying dynamic stats.
            Item.prefix = 0;

            FillingStats stats = CalculateFillingStats();
            DynamicWeaponPrefixHelper.Apply(
                Item,
                stats.Damage,
                stats.Knockback,
                stats.UseTime,
                0,
                20f,
                stats.Crit);

            Item.rare = FillingCount switch
            {
                0 => ItemRarityID.White,
                1 => ItemRarityID.Blue,
                2 => ItemRarityID.Green,
                3 => ItemRarityID.Orange,
                4 => ItemRarityID.LightRed,
                _ => ItemRarityID.Pink
            };
        }

        private FillingStats CalculateFillingStats()
        {
            int damage = 15;
            int useTimePercent = 0;
            float knockback = 5f;
            int crit = 4;
            int projectileCount = 1;
            int penetrationBonus = 0;
            int explosionSize = 0;

            foreach (int frame in GetSelectedFillingFrames())
            {
                if (!BurgerKingFillingCatalog.TryGet(frame, out BurgerKingFillingDefinition filling))
                    continue;

                damage += filling.DamageDelta;
                useTimePercent += filling.UseTimePercentDelta;
                knockback += filling.KnockbackDelta;
                crit += filling.CritDelta;
                projectileCount += filling.ProjectileCountDelta;
                penetrationBonus += filling.PenetrationDelta;
                explosionSize = System.Math.Max(explosionSize, filling.ExplosionSize);
            }

            int useTime = System.Math.Max(1,
                (int)System.Math.Round(20f * (1f + useTimePercent / 100f)));
            return new FillingStats(
                System.Math.Max(1, damage), useTime, knockback, crit,
                System.Math.Max(1, projectileCount), penetrationBonus, explosionSize);
        }

        private void RemoveFilling(int frameIndex)
        {
            List<int> remaining = new(MaximumFillingLayers);
            foreach (int frame in GetSelectedFillingFrames())
            {
                if (frame != frameIndex)
                    remaining.Add(frame);
            }

            filling1 = remaining.Count > 0 ? remaining[0] : 0;
            filling2 = remaining.Count > 1 ? remaining[1] : 0;
            filling3 = remaining.Count > 2 ? remaining[2] : 0;
            filling4 = remaining.Count > 3 ? remaining[3] : 0;
            filling5 = remaining.Count > 4 ? remaining[4] : 0;
        }

        private static void AddIfPresent(List<int> frames, int frame)
        {
            if (frame >= 2 && frame <= 20 && !frames.Contains(frame))
                frames.Add(frame);
        }

        public override void SaveData(TagCompound tag)
        {
            tag["Filling1"] = filling1;
            tag["Filling2"] = filling2;
            tag["Filling3"] = filling3;
            tag["Filling4"] = filling4;
            tag["Filling5"] = filling5;
        }

        public override void LoadData(TagCompound tag)
        {
            filling1 = tag.GetInt("Filling1");
            filling2 = tag.GetInt("Filling2");
            filling3 = tag.GetInt("Filling3");
            filling4 = tag.GetInt("Filling4");
            filling5 = tag.GetInt("Filling5");
            ApplyFillingStats();
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write((byte)filling1);
            writer.Write((byte)filling2);
            writer.Write((byte)filling3);
            writer.Write((byte)filling4);
            writer.Write((byte)filling5);
        }

        public override void NetReceive(BinaryReader reader)
        {
            filling1 = reader.ReadByte();
            filling2 = reader.ReadByte();
            filling3 = reader.ReadByte();
            filling4 = reader.ReadByte();
            filling5 = reader.ReadByte();
            ApplyFillingStats();
        }

        private readonly record struct FillingStats(
            int Damage,
            int UseTime,
            float Knockback,
            int Crit,
            int ProjectileCount,
            int PenetrationBonus,
            int ExplosionSize);

        /// <summary>
        /// Draw order is always bottom bun, up to five filling frames, top bun.
        /// Filling frame indices use the source sheet's zero-based frame number.
        /// </summary>
        public static void DrawBurger(
            SpriteBatch spriteBatch,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            IReadOnlyList<int> fillingFrames = null)
        {
            Texture2D texture = ModContent.Request<Texture2D>(
                "everflow/Content/Items/BurgerKing").Value;

            int fillingCount = fillingFrames == null
                ? 0
                : System.Math.Min(fillingFrames.Count, MaximumFillingLayers);
            Vector2 layerStep = Vector2.UnitY.RotatedBy(rotation) * (LayerRise * scale);
            Vector2 bottomPosition = position + layerStep * ((fillingCount + 1) * 0.5f);

            DrawLayer(spriteBatch, texture, 1, bottomPosition, color, rotation, origin, scale, effects);

            if (fillingFrames != null)
            {
                for (int i = 0; i < fillingCount; i++)
                {
                    int frameIndex = System.Math.Clamp(fillingFrames[i], 2, 20);
                    Vector2 layerPosition = bottomPosition - layerStep * (i + 1);
                    DrawLayer(spriteBatch, texture, frameIndex, layerPosition, color, rotation, origin, scale, effects);
                }
            }

            Vector2 topPosition = bottomPosition - layerStep * (fillingCount + 1);
            DrawLayer(spriteBatch, texture, 0, topPosition, color, rotation, origin, scale, effects);
        }

        private static void DrawLayer(
            SpriteBatch spriteBatch,
            Texture2D texture,
            int frameIndex,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects)
        {
            Rectangle source = new Rectangle(0, frameIndex * FrameStride, FrameWidth, FrameHeight);
            spriteBatch.Draw(texture, position, source, color, rotation, origin, scale, effects, 0f);
        }
    }
}
