using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

using everflow.Content.Items.Materials;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    public class AlloySpear : ModItem
    {
        public override void SetStaticDefaults()
        {
            // 让游戏识别为长矛
            ItemID.Sets.Spears[Type] = true;

            // 允许按住右键连续使用
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;

            // 右键使用时按照法杖方式握持贴图
            // 你的 AlloySpear.png 本身就是左下 -> 右上的法杖式方向
            Item.staff[Type] = true;
        }

        public override void SetDefaults()
        {
            // =========================
            // 基础面板
            // =========================

            Item.width = 52;
            Item.height = 50;

            Item.damage = 20;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 5f;

            // 两种攻击都是25使用时间
            Item.useTime = 25;
            Item.useAnimation = 25;

            Item.useStyle = ItemUseStyleID.Shoot;

            // 武器本体不产生接触伤害
            Item.noMelee = true;

            // 默认按照左键模式设置
            Item.noUseGraphic = true;
            Item.autoReuse = false;

            Item.UseSound = SoundID.Item1;

            // =========================
            // 左键：长矛
            // =========================

            Item.shoot = ModContent.ProjectileType<AlloySpearProjectile>();

            // 控制长矛戳刺的伸出距离/速度
            // 后面可以单独平衡
            Item.shootSpeed = 4.7f;

            // =========================
            // 右键魔法
            // =========================

            // 右键基础耗蓝5
            // 左键会在 ModifyManaCost 中变为0
            Item.mana = 5;

            // =========================
            // 其他
            // =========================

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 50);
        }

        // 开启右键特殊攻击
        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            int spearType = ModContent.ProjectileType<AlloySpearProjectile>();

            // 默认状态始终保持5魔力
            // 这样物品面板由原版正常显示魔力消耗
            Item.mana = 5;

            // =========================
            // 右键：蓝宝石魔弹
            // =========================
            if (player.altFunctionUse == 2)
            {
                Item.useStyle = ItemUseStyleID.Shoot;

                Item.useTime = 25;
                Item.useAnimation = 25;

                Item.noUseGraphic = false;

                // 可以按住右键连射
                Item.autoReuse = true;

                Item.UseSound = SoundID.Item43;

                Item.shoot = ProjectileID.SapphireBolt;
                Item.shootSpeed = 8f;

                // 右键正常消耗5魔力
                Item.mana = 5;

                return true;
            }

            // =========================
            // 左键：长矛戳刺
            // =========================

            Item.useStyle = ItemUseStyleID.Shoot;

            Item.useTime = 25;
            Item.useAnimation = 25;

            Item.noUseGraphic = true;
            Item.autoReuse = false;

            Item.UseSound = SoundID.Item1;

            Item.shoot = spearType;
            Item.shootSpeed = 4.7f;

            // 已经有一根长矛时不允许再次使用
            if (player.ownedProjectileCounts[spearType] > 0)
                return false;

            // 只有真正允许发动左键攻击之后，
            // 才临时把魔力消耗改成0
            Item.mana = 0;

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
            // =========================
            // 左键
            // =========================
            if (player.altFunctionUse != 2)
            {
                // CanUseItem 中临时设成了0
                // 魔力检查完成后恢复成5
                Item.mana = 5;

                return true;
            }

            // =========================
            // 右键
            // =========================

            // 右键应该吃魔法伤害，而不是近战伤害。
            // AlloySpear本体是近战武器，所以这里单独计算魔法伤害。
            int magicDamage = (int)player
                .GetTotalDamage(DamageClass.Magic)
                .ApplyTo(Item.damage);

            int projectileIndex = Projectile.NewProjectile(
                source,
                position,
                velocity,
                ProjectileID.SapphireBolt,
                magicDamage,
                knockback,
                player.whoAmI
            );

            Projectile projectile = Main.projectile[projectileIndex];

            // 魔法伤害
            projectile.DamageType = DamageClass.Magic;

            // 最多命中2次
            projectile.penetrate = 3;
            projectile.maxPenetrate = 3;

            // 每一颗射弹单独记录命中过哪些NPC
            projectile.usesLocalNPCImmunity = true;

            // -1 = 这颗射弹对同一个NPC一生只能造成一次伤害
            projectile.localNPCHitCooldown = -1;

            // 确保不使用同类型Projectile共享免疫
            projectile.usesIDStaticNPCImmunity = false;

            // 防止游戏再自动生成第二颗射弹
            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<AlloyBar>(15)
                .AddIngredient(ItemID.Sapphire, 4)
                .AddTile(TileID.Anvils)
                .Register();
        }
    }
}
