using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using everflow.Content.Players;

namespace everflow.Content.Buffs
{
    public sealed class AquaVirtueBuff_1 : ModBuff
    {
        public override void SetStaticDefaults() => Main.buffNoSave[Type] = true;

        public override void Update(Player player, ref int buffIndex)
        {
            int stacks = player.GetModPlayer<AquaVirtuePlayer>().AquaVirtueVastStacks;
            if (stacks <= 0)
                return;

            player.GetDamage(DamageClass.Magic) += stacks * 0.01f;
            player.GetCritChance(DamageClass.Magic) += stacks;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int stacks = Main.LocalPlayer.GetModPlayer<AquaVirtuePlayer>().AquaVirtueVastStacks;
            tip += "\n" + Language.GetTextValue("Mods.everflow.Buffs.AquaVirtueBuff_1.CurrentBonus", stacks);
        }
    }

    public sealed class AquaVirtueBuff_2 : ModBuff
    {
        public override void SetStaticDefaults() => Main.buffNoSave[Type] = true;

        public override void Update(Player player, ref int buffIndex)
        {
            // 百分比移动速度与总防御力必须在玩家装备属性完成结算后应用，
            // 具体数值由 AquaVirtuePlayer.PostUpdateMiscEffects 处理。
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int stacks = Main.LocalPlayer.GetModPlayer<AquaVirtuePlayer>().AquaVirtueSurgingStacks;
            tip += "\n" + Language.GetTextValue("Mods.everflow.Buffs.AquaVirtueBuff_2.CurrentBonus", stacks);
        }
    }
}
