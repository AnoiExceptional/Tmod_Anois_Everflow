using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    /// <summary>
    /// 合并原版夜猫子与狩猎Buff的玩家状态，不额外叠加两个原版Buff图标。
    /// </summary>
    public sealed class IRSightBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // 原版夜猫子Buff的核心效果。
            player.nightVision = true;
            // 原版狩猎Buff的核心效果。
            player.detectCreature = true;
        }
    }
}
