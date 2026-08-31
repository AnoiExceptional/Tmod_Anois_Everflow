using everflow.Content.Players;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    public sealed class YaZiDebuff : ModBuff
    {
        public override string Texture => "everflow/Content/Buffs/YaZiDebuff";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            YaZiRetributionPlayer state = player.GetModPlayer<YaZiRetributionPlayer>();
            if (state.Stacks <= 0)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            // 该Debuff由受到睚眦伤害或阶段转换移除，不随时间自然结束。
            player.buffTime[buffIndex] = 18000;
        }

        public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare)
        {
            int stacks = Main.LocalPlayer
                .GetModPlayer<YaZiRetributionPlayer>()
                .Stacks;
            tip = $"当前{stacks}/{YaZiRetributionPlayer.MaximumStacks}层；受到睚眦的下次伤害提高{stacks * 10}%";
        }
    }
}
