using System;
using everflow.Content.Items.Accessories;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace everflow.Content.Systems
{
    public sealed class SinBrandAcquisitionSystem : ModSystem
    {
        public static bool WallOfFleshBrandAwarded;

        public override void ClearWorld()
        {
            WallOfFleshBrandAwarded = false;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (WallOfFleshBrandAwarded)
                tag[nameof(WallOfFleshBrandAwarded)] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            WallOfFleshBrandAwarded = tag.GetBool(nameof(WallOfFleshBrandAwarded));

            // Existing hardmode worlds have already had their first Wall of Flesh kill.
            if (!tag.ContainsKey(nameof(WallOfFleshBrandAwarded)) && Main.hardMode)
                WallOfFleshBrandAwarded = true;
        }

        public override void PostWorldGen()
        {
            Chest deepestLockedGoldChest = null;
            int deepestY = -1;

            for (int i = 0; i < Main.maxChests; i++)
            {
                Chest chest = Main.chest[i];
                if (chest == null || chest.y <= deepestY)
                    continue;

                Tile tile = Main.tile[chest.x, chest.y];
                if (!tile.HasTile || tile.TileType != TileID.Containers ||
                    tile.TileFrameX / 36 != 2)
                    continue;

                deepestLockedGoldChest = chest;
                deepestY = chest.y;
            }

            if (deepestLockedGoldChest == null)
                return;

            for (int slot = 0; slot < Chest.maxItems; slot++)
            {
                if (!deepestLockedGoldChest.item[slot].IsAir)
                    continue;

                deepestLockedGoldChest.item[slot].SetDefaults(
                    ModContent.ItemType<SinBrand_Pride>());
                break;
            }
        }
    }

    public sealed class SinBrandBossDrops : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.QueenBee)
                npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.Common(
                    ModContent.ItemType<SinBrand_Gluttony>(), 2));

            if (npc.type == NPCID.HallowBoss)
            {
                npcLoot.Add(Terraria.GameContent.ItemDropRules.ItemDropRule.ByCondition(
                    new NighttimeEmpressCondition(),
                    ModContent.ItemType<SinBrand_Lust>(), 3));
            }
        }

        public override void OnKill(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (npc.type == NPCID.WallofFlesh)
            {
                bool shouldDrop = !SinBrandAcquisitionSystem.WallOfFleshBrandAwarded ||
                    Main.rand.NextBool(7);
                SinBrandAcquisitionSystem.WallOfFleshBrandAwarded = true;
                if (shouldDrop)
                    DropBrand(npc, ModContent.ItemType<SinBrand_Wrath>());
            }

            if (npc.type == NPCID.PirateShip && NPC.downedGolemBoss)
            {
                Player player = GetResponsiblePlayer(npc);
                int bonuses = CountGreedAccessories(player);
                float chance = Math.Min(1f, 0.01f + bonuses * 0.33f);
                if (Main.rand.NextFloat() < chance)
                    DropBrand(npc, ModContent.ItemType<SinBrand_Greed>());
            }
        }

        private static void DropBrand(NPC npc, int itemType)
        {
            Item.NewItem(npc.GetSource_Loot(), npc.getRect(), itemType);
        }

        private static Player GetResponsiblePlayer(NPC npc)
        {
            int index = npc.lastInteraction;
            if (index >= 0 && index < Main.maxPlayers && Main.player[index].active)
                return Main.player[index];

            return Main.player[Player.FindClosest(npc.position, npc.width, npc.height)];
        }

        private static int CountGreedAccessories(Player player)
        {
            bool goldRing = false;
            bool luckyCoin = false;
            bool discountCard = false;

            for (int slot = 3; slot < Math.Min(10, player.armor.Length); slot++)
            {
                int type = player.armor[slot].type;
                goldRing |= type == ItemID.GoldRing;
                luckyCoin |= type == ItemID.LuckyCoin;
                discountCard |= type == ItemID.DiscountCard;
            }

            return (goldRing ? 1 : 0) + (luckyCoin ? 1 : 0) +
                (discountCard ? 1 : 0);
        }
    }

    public sealed class NighttimeEmpressCondition :
        Terraria.GameContent.ItemDropRules.IItemDropRuleCondition
    {
        public bool CanDrop(Terraria.GameContent.ItemDropRules.DropAttemptInfo info) => !Main.dayTime;
        public bool CanShowItemDropInUI() => true;
        public string GetConditionDescription() => "Drops only when defeated at night";
    }
}
