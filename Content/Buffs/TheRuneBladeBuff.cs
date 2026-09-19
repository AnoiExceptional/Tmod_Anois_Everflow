using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Buffs
{
    public sealed class TheRuneBladeBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // 强化状态不得跨世界保存；重新进入世界时若残留强化物品，
            // 物品自身会因缺少此Buff立即恢复普通形态。
            Main.buffNoSave[Type] = true;
        }
    }
}
