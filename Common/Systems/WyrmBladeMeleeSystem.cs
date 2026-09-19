using everflow.Content.Items;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Common.Systems
{
    /// <summary>
    /// 让强化连阙剑保持原版物品近战结算，同时允许一次挥舞继续扫描
    /// 剑刃范围内的全部NPC。每个NPC自己的meleeNPCHitCooldown仍由原版处理。
    /// </summary>
    public sealed class WyrmBladeMeleeSystem : ModSystem
    {
        public override void Load()
        {
            On_Player.ApplyAttackCooldown += SkipSingleTargetCooldown;
        }

        public override void Unload()
        {
            On_Player.ApplyAttackCooldown -= SkipSingleTargetCooldown;
        }

        private static void SkipSingleTargetCooldown(
            On_Player.orig_ApplyAttackCooldown orig,
            Player player)
        {
            if (player.HeldItem.type ==
                    ModContent.ItemType<WyrmBladeGreatswordEmpowered>() &&
                player.itemAnimation > 0)
            {
                // ProcessHitAgainstNPC会在每次成功命中末尾调用此方法，并把
                // attackCD设为动画时长的约1/3；随后的NPC因此无法进入结算。
                // 仅跳过这项“玩家全局”冷却，目标各自的近战免疫保持不变。
                return;
            }

            orig(player);
        }
    }
}
