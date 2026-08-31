using everflow.Common.Items;
using everflow.Content.Buffs;
using everflow.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Accessories
{
    public sealed class SinBrand : FoundationAccessory
    {
        public override bool CanAccessoryBeEquippedWith(
            Item equippedItem,
            Item incomingItem,
            Player player)
        {
            if (!base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player))
                return false;

            bool equippedIsSinBrand = equippedItem.ModItem is SinBrand;
            bool incomingIsSinBrand = incomingItem.ModItem is SinBrand;
            bool equippedIsComponent = equippedItem.ModItem is SinBrandAccessory;
            bool incomingIsComponent = incomingItem.ModItem is SinBrandAccessory;

            return !((equippedIsSinBrand && incomingIsComponent) ||
                (incomingIsSinBrand && equippedIsComponent));
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = Item.sellPrice(gold: 20);
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions -= 1;
            player.statManaMax2 -= 180;
            player.GetDamage(DamageClass.Summon) += 0.30f;
            player.GetCritChance(DamageClass.Summon) += 25f;

            SinBrandPlayer brandPlayer = player.GetModPlayer<SinBrandPlayer>();
            brandPlayer.ParadiseLost = true;
            brandPlayer.Gluttony = true;
            brandPlayer.Jealousy = true;
            brandPlayer.Sloth = true;
            brandPlayer.Wrath = true;
            player.AddBuff(ModContent.BuffType<SinBrandBuff>(), 2);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<SinBrand_Gluttony>()
                .AddIngredient<SinBrand_Pride>()
                .AddIngredient<SinBrand_Wrath>()
                .AddIngredient<SinBrand_Jealous>()
                .AddIngredient<SinBrand_Lust>()
                .AddIngredient<SinBrand_Greed>()
                .AddIngredient<SinBrand_Sloth>()
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }

    public abstract class SinBrandAccessory : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 5);
        }

        protected static SinBrandPlayer BrandPlayer(Player player) =>
            player.GetModPlayer<SinBrandPlayer>();
    }

    public sealed class SinBrand_Gluttony : SinBrandAccessory
    {
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions -= 1;
            BrandPlayer(player).Gluttony = true;
        }
    }

    public sealed class SinBrand_Pride : SinBrandAccessory
    {
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions -= 1;
            BrandPlayer(player).Pride = true;
        }
    }

    public sealed class SinBrand_Wrath : SinBrandAccessory
    {
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions -= 1;
            player.GetCritChance(DamageClass.Summon) += 25f;
            BrandPlayer(player).Wrath = true;
        }
    }

    public sealed class SinBrand_Jealous : SinBrandAccessory
    {
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions -= 1;
            BrandPlayer(player).Jealousy = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SoulofMight, 13)
                .AddIngredient(ItemID.SoulofSight, 13)
                .AddIngredient(ItemID.SoulofFright, 13)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public sealed class SinBrand_Lust : SinBrandAccessory
    {
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions -= 1;
            BrandPlayer(player).Lust = true;
        }
    }

    public sealed class SinBrand_Greed : SinBrandAccessory
    {
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions += 3;
            player.statManaMax2 -= 180;
        }
    }

    public sealed class SinBrand_Sloth : SinBrandAccessory
    {
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.maxMinions -= 1;
            BrandPlayer(player).Sloth = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.LunarBar, 100)
                .AddIngredient(ItemID.ChlorophyteBar, 100)
                .AddIngredient(ItemID.HallowedBar, 100)
                .AddTile(TileID.LunarCraftingStation)
                .Register();
        }
    }
}
