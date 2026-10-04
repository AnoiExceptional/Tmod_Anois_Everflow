using everflow.Content.Items;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace everflow.Content.Systems
{
    public sealed class ZhangEJadeGlobalNPC : GlobalNPC
    {
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.friendly || npc.townNPC || npc.lifeMax <= 5 ||
                NPCID.Sets.CountsAsCritter[npc.type])
            {
                return;
            }

            npcLoot.Add(ItemDropRule.ByCondition(
                new ZhangEJadeDropCondition(),
                ModContent.ItemType<ZhangEJade>(),
                100));
        }
    }

    public sealed class ZhangEJadeDropCondition : IItemDropRuleCondition
    {
        public bool CanDrop(DropAttemptInfo info)
        {
            float tileY = info.npc.Center.Y / 16f;
            return NPC.downedPlantBoss &&
                tileY > Main.worldSurface &&
                tileY < Main.UnderworldLayer;
        }

        public bool CanShowItemDropInUI() => true;

        public string GetConditionDescription() => Language.GetTextValue(
            "Mods.everflow.DropConditions.ZhangEJade");
    }
}
