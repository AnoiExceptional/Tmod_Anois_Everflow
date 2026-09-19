using Terraria.ModLoader;

namespace everflow.Common.Players
{
    /// <summary>
    /// 保存跨越连阙剑普通/强化两个Item Type的右键松手闩锁。
    /// </summary>
    public sealed class WyrmBladeInputPlayer : ModPlayer
    {
        public bool RightReleaseRequired { get; private set; }
        private bool rawRightHeld;

        public void RequireRightRelease()
        {
            RightReleaseRequired = true;
        }

        public override void SetControls()
        {
            // 必须先保存未经修改的物理输入。闩锁期间仅对物品使用系统隐藏
            // 这次持续按住的右键，不能把隐藏后的false误认为玩家已经松手。
            rawRightHeld = Player.controlUseTile;
            if (RightReleaseRequired && rawRightHeld)
                Player.controlUseTile = false;
        }

        public override void PostUpdate()
        {
            // 只有真实物理右键松开后才解除。下一次新按下会正常传递给物品，
            // 从而发动旋斩或重新开始形态转换。
            if (RightReleaseRequired && !rawRightHeld)
                RightReleaseRequired = false;
        }
    }
}
