using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using everflow.Common;
using everflow.Common.Players;
using everflow.Content.Projectiles;

namespace everflow.Content.Items
{
    public class DragonsEdge : ModItem
    {
        private bool rightAttackActive;

        public readonly struct StageStats
        {
            public readonly int LeftDamage;
            public readonly int LeftUseTime;
            public readonly int SlashDamage;
            public readonly float SlashRangeTiles;
            public readonly int SlashPierceCount;
            public readonly int RightDamage;
            public readonly int RightUseTime;
            public readonly float RightShootSpeed;

            public StageStats(
                int leftDamage,
                int leftUseTime,
                int slashDamage,
                float slashRangeTiles,
                int slashPierceCount,
                int rightDamage,
                int rightUseTime,
                float rightShootSpeed)
            {
                LeftDamage = leftDamage;
                LeftUseTime = leftUseTime;
                SlashDamage = slashDamage;
                SlashRangeTiles = slashRangeTiles;
                SlashPierceCount = slashPierceCount;
                RightDamage = rightDamage;
                RightUseTime = rightUseTime;
                RightShootSpeed = rightShootSpeed;
            }
        }

        public static int GetProgressionStage()
        {
            if (NPC.downedMoonlord)
                return 8;
            if (NPC.downedAncientCultist)
                return 7;
            if (NPC.downedPlantBoss)
                return 6;
            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3)
                return 5;
            if (Main.hardMode)
                return 4;
            if (NPC.downedBoss3)
                return 3;

            bool defeatedFirstBoss =
                NPC.downedSlimeKing ||
                NPC.downedBoss1 ||
                NPC.downedBoss2 ||
                NPC.downedQueenBee ||
                NPC.downedDeerclops;
            return defeatedFirstBoss ? 2 : 1;
        }

        public static StageStats GetStageStats()
        {
            return GetProgressionStage() switch
            {
                1 => new StageStats(30, 25, 0, 0f, 0, 35, 30, 5f),
                2 => new StageStats(40, 20, 0, 0f, 0, 60, 30, 6f),
                3 => new StageStats(40, 15, 20, 30f, 1, 75, 25, 7f),
                4 => new StageStats(40, 15, 30, 35f, 1, 90, 20, 8f),
                5 => new StageStats(45, 15, 45, 45f, 1, 110, 20, 9f),
                6 => new StageStats(75, 15, 75, 60f, 2, 175, 20, 10f),
                7 => new StageStats(100, 10, 75, 0f, 3, 200, 20, 12f),
                _ => new StageStats(150, 10, 150, 0f, 4, 225, 20, 15f)
            };
        }

        private static int GetStageRarity()
        {
            return GetProgressionStage() switch
            {
                1 => ItemRarityID.Blue,
                2 => ItemRarityID.Green,
                3 => ItemRarityID.Orange,
                4 => ItemRarityID.LightRed,
                5 => ItemRarityID.Pink,
                6 => ItemRarityID.Lime,
                7 => ItemRarityID.Red,
                _ => ItemRarityID.Purple
            };
        }

        private static string Text(string key, params object[] args) =>
            Language.GetTextValue($"Mods.everflow.Items.DragonsEdge.Dynamic.{key}", args);

        private static string GetUnlockedAttacks(int stage)
        {
            List<string> attacks = new();
            if (stage >= 3)
                attacks.Add(Text("BladeWave"));
            if (stage >= 4)
                attacks.Add(Text("Wyvern"));
            if (stage >= 7)
                attacks.Add(Text("StardustDragon"));
            if (stage >= 8)
                attacks.Add(Text("PhantasmDragon"));

            return attacks.Count == 0
                ? Text("NoneUnlocked")
                : string.Join(Text("ListSeparator"), attacks);
        }

        private static string GetNextSummary(int stage) => stage switch
        {
            1 => Text("NextFirstBoss"),
            2 => Text("NextSkeletron"),
            3 => Text("NextWallOfFlesh"),
            4 => Text("NextMechBoss"),
            5 => Text("NextPlantera"),
            6 => Text("NextCultist"),
            7 => Text("NextMoonLord"),
            _ => string.Empty
        };

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 96;
            Item.height = 98;
            Item.damage = 15;
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 7f;
            Item.crit = 9;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.shoot = ModContent.ProjectileType<DragonsEdgeWhipProjectile>();
            Item.shootSpeed = 8f;
            Item.value = Item.sellPrice(gold: 1);
            Item.rare = GetStageRarity();

            ResetToMelee();
        }

        public override void UpdateInventory(Player player)
        {
            // 稀有度随世界进度实时更新，即使武器正在攻击也应立即生效。
            Item.rare = GetStageRarity();

            // 攻击进行中不能覆盖已锁定的右键属性。
            if (ReferenceEquals(player.HeldItem, Item) && player.itemAnimation > 0)
                return;

            ResetToMelee();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.RemoveAll(line =>
                line.Mod == Mod.Name && line.Name.StartsWith("Tooltip"));

            int stage = GetProgressionStage();

            tooltips.Add(new TooltipLine(Mod, "CurrentStage",
                Text("CurrentStage", stage)));
            tooltips.Add(new TooltipLine(Mod, "CurrentUnlocks",
                Text("CurrentUnlocks", GetUnlockedAttacks(stage))));

            if (stage < 8)
            {
                tooltips.Add(new TooltipLine(Mod, "NextSummary",
                    GetNextSummary(stage)));
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "FullyAwakened",
                    Text("FullyAwakened")));
            }

            tooltips.Add(new TooltipLine(Mod, "Flavor",
                Text("Flavor"))
            {
                OverrideColor = new Color(255, 80, 190)
            });
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.None)
                return false;

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.Right)
            {
                StageStats stats = GetStageStats();
                rightAttackActive = true;
                Item.DamageType = DamageClass.SummonMeleeSpeed;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.UseSound = SoundID.Item71;
                DynamicWeaponPrefixHelper.Apply(
                    Item,
                    stats.RightDamage,
                    9f,
                    stats.RightUseTime,
                    0,
                    stats.RightShootSpeed);
            }
            else
            {
                ResetToMelee();
            }

            return true;
        }

        public override void UseAnimation(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();
            rightAttackActive =
                input.Mode == AlloySwordInputPlayer.SwordInputMode.Right;

            if (rightAttackActive)
            {
                Item.noUseGraphic = true;
                Item.useStyle = ItemUseStyleID.Swing;
            }
            else
            {
                ResetToMelee();
            }
        }

        public override void HoldItem(Player player)
        {
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            if (rightAttackActive &&
                input.Mode != AlloySwordInputPlayer.SwordInputMode.Right &&
                player.itemAnimation <= 0)
            {
                ResetToMelee();
            }
            else if (player.itemAnimation <= 0)
            {
                ResetToMelee();
            }
        }

        public override void UseItemHitbox(
            Player player,
            ref Rectangle hitbox,
            ref bool noHitbox)
        {
            if (rightAttackActive)
                noHitbox = true;
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (rightAttackActive || player.itemAnimation <= 0)
                return;

            // 较弱的粉色动态光源，随剑的近战伤害框移动。
            Lighting.AddLight(
                hitbox.Center.ToVector2(),
                new Vector3(0.28f, 0.07f, 0.20f));

            // 星怒/附魔剑风格的粉色星光粒子。
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustDirect(
                    hitbox.TopLeft(),
                    hitbox.Width,
                    hitbox.Height,
                    DustID.Enchanted_Pink,
                    player.velocity.X * 0.15f,
                    player.velocity.Y * 0.15f,
                    80,
                    Color.LightPink,
                    Main.rand.NextFloat(0.7f, 1.0f));

                dust.noGravity = true;
                dust.fadeIn = 1.05f;
                dust.velocity += Main.rand.NextVector2Circular(0.8f, 0.8f);
            }
        }

        public override void OnHitNPC(
            Player player,
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            if (rightAttackActive || player.whoAmI != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                player.GetSource_ItemUse(Item),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<DragonsEdgeHitSlashProjectile>(),
                0,
                0f,
                player.whoAmI,
                0f,
                Main.rand.NextFloat(-MathHelper.ToRadians(12f),
                    MathHelper.ToRadians(12f)));
        }

        private void ResetToMelee()
        {
            StageStats stats = GetStageStats();
            rightAttackActive = false;
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = false;
            Item.noUseGraphic = false;
            Item.shoot = ModContent.ProjectileType<DragonsEdgeWhipProjectile>();
            DynamicWeaponPrefixHelper.Apply(
                Item,
                stats.LeftDamage,
                7f,
                stats.LeftUseTime,
                0,
                stats.RightShootSpeed);
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
            AlloySwordInputPlayer input =
                player.GetModPlayer<AlloySwordInputPlayer>();

            if (input.Mode == AlloySwordInputPlayer.SwordInputMode.Right)
                return true;

            // 骷髅王后才解锁左键剑气。
            if (GetProgressionStage() < 3)
                return false;

            Vector2 slashSpawnPosition = player.RotatedRelativePoint(
                player.MountedCenter,
                reverseRotation: true);
            Vector2 slashVelocity = (Main.MouseWorld - slashSpawnPosition)
                .SafeNormalize(Vector2.UnitX * player.direction) * 17f;
            StageStats stats = GetStageStats();
            int slashDamage = (int)(damage *
                (stats.SlashDamage / (float)stats.LeftDamage));
            float maxRangePixels = stats.SlashRangeTiles * 16f;

            Projectile.NewProjectile(
                source,
                slashSpawnPosition,
                slashVelocity,
                ModContent.ProjectileType<DragonsEdgeSlashProjectile>(),
                slashDamage,
                knockback,
                player.whoAmI,
                maxRangePixels,
                stats.SlashPierceCount);

            return false;
        }
    }
}
