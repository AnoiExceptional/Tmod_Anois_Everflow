using System;
using System.Collections.Generic;
using everflow.Content.Items;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace everflow.Content.Systems
{
    public sealed class YaZiBrokenDaggerDropGlobalNPC : GlobalNPC
    {
        // 使用内部英文名明确分类。蠕虫型敌怪只登记头部，避免身体节
        // 分别进行掉落判定。
        private static readonly HashSet<string> ArthropodNpcNames = new(
            StringComparer.Ordinal)
        {
            "Bee", "BeeSmall", "QueenBee",
            "Hornet", "HornetFatty", "HornetHoney", "HornetLeafy",
            "HornetSpikey", "HornetStingy", "MossHornet",
            "Moth", "Mothron", "MothronSpawn", "Derpling",
            "WallCreeper", "WallCreeperWall",
            "JungleCreeper", "JungleCreeperWall",
            "BlackRecluse", "BlackRecluseWall",
            "BloodCrawler", "BloodCrawlerWall",
            "Antlion", "GiantAntlion", "WalkingAntlion",
            "FlyingAntlion", "AntlionCharger", "AntlionSwarmer",
            "SandPoacher", "TombCrawlerHead", "DuneSplicerHead",
            "Crawdad", "Crawdad2", "Crab", "GraniteFlyer",
            "CrawltipedeHead", "StardustSpiderBig",
            "StardustSpiderSmall", "MushiLadybug"
        };

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            string internalName = NPCID.Search.GetName(npc.type);
            if (!ArthropodNpcNames.Contains(internalName))
                return;

            npcLoot.Add(ItemDropRule.ByCondition(
                new YaZiBrokenDaggerDropCondition(),
                ModContent.ItemType<YaZiBrokenDagger>(),
                100));
        }
    }

    public sealed class YaZiBrokenDaggerDropCondition :
        IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info) => NPC.downedPlantBoss;

        public bool CanShowItemDropInUI() => true;

        public string GetConditionDescription() => Language.GetTextValue(
            "Mods.everflow.DropConditions.YaZiBrokenDagger");
    }
}
