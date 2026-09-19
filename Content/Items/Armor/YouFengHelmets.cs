using everflow.Content.Items.Materials;
using everflow.Content.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Armor
{
    public abstract class YouFengHeadpiece : ModItem
    {
        protected abstract int Defense { get; }
        protected virtual float MeleeDamage => 0.15f;
        protected virtual float CritChance => 0f;
        protected virtual float RangedDamage => 0f;
        protected virtual float MagicDamage => 0f;
        protected virtual float SummonDamage => 0f;

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 20;
            Item.defense = Defense;
            Item.rare = ItemRarityID.Lime;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += MeleeDamage;
            player.GetDamage(DamageClass.Ranged) += RangedDamage;
            player.GetDamage(DamageClass.Magic) += MagicDamage;
            player.GetDamage(DamageClass.Summon) += SummonDamage;
            player.GetCritChance(DamageClass.Generic) += CritChance;
        }

        public override bool IsArmorSet(Item head, Item body, Item legs) =>
            body.type == ModContent.ItemType<YouFengBreastplate>() &&
            legs.type == ModContent.ItemType<YouFengLeggings>();

        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = this.GetLocalization("SetBonus").Value;
            player.GetModPlayer<YouFengArmorPlayer>().SetActive = true;
            ApplyFixedSetBonus(player);
        }

        protected abstract void ApplyFixedSetBonus(Player player);

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<WyrmBladeBar>(8)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    [AutoloadEquip(EquipType.Head)]
    public sealed class YouFengHeadgear : YouFengHeadpiece
    {
        protected override int Defense => 25;

        protected override void ApplyFixedSetBonus(Player player)
        {
            player.statDefense += 15;
        }
    }

    [AutoloadEquip(EquipType.Head)]
    public sealed class YouFengHelmet : YouFengHeadpiece
    {
        protected override int Defense => 18;
        protected override float MeleeDamage => 0.20f;
        protected override float CritChance => 10f;
        protected override float RangedDamage => 0.15f;

        protected override void ApplyFixedSetBonus(Player player)
        {
            player.GetAttackSpeed(DamageClass.Generic) += 0.15f;
        }
    }

    [AutoloadEquip(EquipType.Head)]
    public sealed class YouFengMask : YouFengHeadpiece
    {
        protected override int Defense => 18;
        protected override float CritChance => 15f;
        protected override float MagicDamage => 0.10f;

        protected override void ApplyFixedSetBonus(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += 15f;
        }
    }

    [AutoloadEquip(EquipType.Head)]
    public sealed class YouFengHat : YouFengHeadpiece
    {
        protected override int Defense => 10;
        protected override float CritChance => 10f;
        protected override float SummonDamage => 0.10f;

        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawFullHair[Item.headSlot] = true;
        }

        protected override void ApplyFixedSetBonus(Player player)
        {
            player.moveSpeed += 0.20f;
        }
    }
}
