using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Players;
using everflow.Content.Buffs;
using everflow.Content.Items.Materials;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    public class AlloyWand : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            // 贴图来自促动器魔杖，沿用其手持尺寸与握持基准。
            Item.CloneDefaults(ItemID.ActuationRod);
            Item.damage = 15;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 10;
            Item.knockBack = 1f;
            Item.noMelee = true;
            Item.noUseGraphic = false;
            // 只借用促动器魔杖的贴图握持基准，不继承其工具和放置能力。
            Item.mech = false;
            Item.tileBoost = 0;
            Item.pick = 0;
            Item.axe = 0;
            Item.hammer = 0;
            Item.tileWand = -1;
            Item.createTile = -1;
            Item.createWall = -1;
            Item.placeStyle = 0;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item8;
            Item.shoot = ModContent.ProjectileType<AlloyWandProjectile>();
            Item.shootSpeed = 32f;
            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.None)
                return false;

            bool conduct = input.Mode == AlloySwordInputPlayer.SwordInputMode.Right;
            // 面板始终显示正常的15点魔法伤害；右键没有近战判定也不会发射弹幕。
            Item.damage = 15;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = conduct ? ItemUseStyleID.HoldUp : ItemUseStyleID.Swing;
            Item.UseSound = conduct ? SoundID.Item29 : SoundID.Item8;
            Item.shoot = conduct
                ? ProjectileID.None
                : ModContent.ProjectileType<AlloyWandProjectile>();

            return true;
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            if (player.GetModPlayer<AlloySwordInputPlayer>().Mode ==
                AlloySwordInputPlayer.SwordInputMode.Right)
            {
                mult = 0f;
            }
        }

        public override bool? UseItem(Player player)
        {
            if (player.GetModPlayer<AlloySwordInputPlayer>().Mode !=
                AlloySwordInputPlayer.SwordInputMode.Right)
            {
                return null;
            }

            AlloyConductPlayer conductPlayer = player.GetModPlayer<AlloyConductPlayer>();
            int effectiveMaxMinions = player.maxMinions;
            if (player.HasBuff<AlloyConduct>())
                effectiveMaxMinions += conductPlayer.ReservedMinionSlots;

            conductPlayer.ReservedMinionSlots = Math.Max(
                0,
                (int)Math.Floor(effectiveMaxMinions - player.slotsMinions + 0.001f));

            player.AddBuff(ModContent.BuffType<AlloyConduct>(), 10 * 60);
            return true;
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
            return player.GetModPlayer<AlloySwordInputPlayer>().Mode !=
                AlloySwordInputPlayer.SwordInputMode.Right;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(15)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
