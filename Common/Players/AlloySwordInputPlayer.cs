using Terraria;
using Terraria.ModLoader;
using everflow.Content.Items;
using everflow.Content.Projectiles;

namespace everflow.Common.Players
{
    public class AlloySwordInputPlayer : ModPlayer
    {
        public enum SwordInputMode : byte
        {
            None,
            Left,
            Right
        }

        public SwordInputMode Mode { get; private set; }
            = SwordInputMode.None;


        public override void SetControls()
        {
            // 先保存这一帧原始输入
            bool leftHeld = Player.controlUseItem;
            bool rightHeld = Player.controlUseTile;


            // 合金大剑和合金链剑共用左右键互斥逻辑。
            int heldItemType = Player.HeldItem.type;
            bool usesExclusiveInput =
                heldItemType == ModContent.ItemType<AlloySword>() ||
                heldItemType == ModContent.ItemType<AlloyChainsword>() ||
                heldItemType == ModContent.ItemType<DragonsEdge>() ||
                heldItemType == ModContent.ItemType<ThousandBlade>() ||
                heldItemType == ModContent.ItemType<AlloyDagger>() ||
                heldItemType == ModContent.ItemType<AlloyXbow>() ||
                heldItemType == ModContent.ItemType<AlloyWand>();

            if (!usesExclusiveInput)
            {
                Mode = SwordInputMode.None;
                return;
            }


            switch (Mode)
            {
                // =========================
                // 当前没有按键占用
                // =========================
                case SwordInputMode.None:

                    // 两个同时第一次按下时：
                    // 这里规定左键优先
                    if (leftHeld)
                    {
                        Mode = SwordInputMode.Left;
                    }
                    else if (rightHeld)
                    {
                        Mode = SwordInputMode.Right;
                    }

                    break;


                // =========================
                // 左键已经占用
                // =========================
                case SwordInputMode.Left:

                    // 只要左键还没松，
                    // 右键永远不能抢走控制权
                    if (!leftHeld)
                    {
                        Mode = rightHeld
                            ? SwordInputMode.Right
                            : SwordInputMode.None;
                    }

                    break;


                // =========================
                // 右键已经占用
                // =========================
                case SwordInputMode.Right:

                    // 只要右键还没松，
                    // 左键永远不能抢走控制权
                    if (!rightHeld)
                    {
                        Mode = leftHeld
                            ? SwordInputMode.Left
                            : SwordInputMode.None;
                    }

                    break;
            }


            // =========================
            // 真正屏蔽另一个输入
            // =========================

            if (Mode == SwordInputMode.Left)
            {
                // 左键占用时：
                // Terraria根本看不到右键输入
                Player.controlUseTile = false;
            }
            else if (Mode == SwordInputMode.Right)
            {
                // 右键占用时：
                // Terraria根本看不到左键输入
                Player.controlUseItem = false;
            }
        }

        public override void PostUpdate()
        {
            if (Player.itemAnimation <= 0 ||
                Player.itemAnimationMax <= 0 ||
                Player.HeldItem.type != ModContent.ItemType<ThousandBlade>() ||
                !HasActiveReversedThousandBladeWhip())
            {
                return;
            }

            // 原版Swing使用身体帧1 -> 2 -> 3。反向链刃只倒放视觉帧，
            // 保持itemAnimation正常递减，避免改变弹幕寿命和伤害时机。
            float animationRatio =
                Player.itemAnimation /
                (float)Player.itemAnimationMax;

            int reversedFrame = animationRatio > 0.666f
                ? 3
                : animationRatio > 0.333f
                    ? 2
                    : 1;

            // PostUpdate在当前版本中位于PlayerFrame之后，只写入一次最终帧，
            // 不会再与绘制阶段或原版PlayerFrame互相覆盖。
            Player.bodyFrame.Y = Player.bodyFrame.Height * reversedFrame;
        }

        private bool HasActiveReversedThousandBladeWhip()
        {
            int projectileType =
                ModContent.ProjectileType<ThousandBladeWhipProjectile>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active &&
                    projectile.owner == Player.whoAmI &&
                    projectile.type == projectileType &&
                    projectile.ai[2] == 1f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
