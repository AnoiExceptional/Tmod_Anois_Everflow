using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Buffs;
using everflow.Common.Players;

namespace everflow.Content.Mounts
{
    public class AlloyTank02 : ModMount
    {
        // 在原先1.5倍视觉尺寸的基础上再次放大1.5倍。
        public const float TankDrawScale = 2.25f;
        public const float WeaponDrawScale = TankDrawScale * 0.5f;
        public const float WeaponPivotOffset = 32f;
        public const float TurretVerticalOffset = -5f;
        public const float WeaponVerticalOffset = 4f;
        public const float WeaponForwardOffset = 16f;
        // In the original 76x48 composite frame the 28x16 turret occupied
        // (32, 6). Its center is therefore eight source pixels to the right
        // of the hull frame center. Mirroring this offset keeps the turret
        // attached to the same point on either side.
        public const float TurretCenterOffset = 8f * TankDrawScale;
        // AlloyTank02_Weapon.png 宽40像素，炮口位于缩放后贴图的右端。
        public const float WeaponLength = 40f * WeaponDrawScale + WeaponForwardOffset;

        /// <summary>
        /// 坦克各分离图层共用的视觉脚底锚点。gfxOffY包含半砖、斜坡等
        /// 地形造成的逐帧平滑偏移，必须和原版坐骑车体绘制使用同一份值。
        /// </summary>
        public static Vector2 GetVisualFeetWorld(Player player) =>
            player.Bottom - new Vector2(0f, player.mount.PlayerOffset) +
            Vector2.UnitY * player.gfxOffY;

        public static Vector2 GetTurretCenterWorld(Player player) =>
            GetVisualFeetWorld(player) +
            new Vector2(0f, TurretVerticalOffset);

        public static Vector2 GetWeaponPivotWorld(Player player) =>
            GetTurretCenterWorld(player) +
            new Vector2(0f, WeaponVerticalOffset);

        public static Vector2 RoundScreenPosition(Vector2 worldPosition)
        {
            Vector2 screenPosition = worldPosition - Main.screenPosition;
            screenPosition.X = (float)System.Math.Round(screenPosition.X);
            screenPosition.Y = (float)System.Math.Round(screenPosition.Y);
            return screenPosition;
        }

        public override void SetStaticDefaults()
        {
            // 4.9 像素/刻约等于25 mph；禁止冲刺状态突破该上限。
            MountData.runSpeed = 4.9f;
            MountData.dashSpeed = 4.9f;
            MountData.acceleration = 0.10f;

            MountData.jumpHeight = 8;
            MountData.jumpSpeed = 4f;
            MountData.constantJump = false;
            MountData.blockExtraJumps = false;
            MountData.flightTimeMax = 0;
            MountData.fallDamage = 0.5f;

            MountData.buff = ModContent.BuffType<AlloyTank02Buff>();
            MountData.spawnDust = DustID.TintableDust;
            MountData.spawnDustNoGravity = false;

            MountData.totalFrames = 4;
            // 驾驶员相对车体上移48像素（3格），只从车顶露出头部。
            MountData.playerYOffsets = Enumerable.Repeat(66, 4).ToArray();
            MountData.xOffset = 0;
            // 车体保持原始贴地基准；playerYOffsets 只负责驾驶员相对位置。
            MountData.yOffset = 0;
            MountData.playerHeadOffset = 66;
            // 恢复原始落地碰撞高度，避免坐骑整体被碰撞箱向上顶起。
            MountData.heightBoost = 24;
            MountData.bodyFrame = 3;

            MountData.standingFrameStart = 0;
            MountData.standingFrameCount = 1;
            MountData.standingFrameDelay = 12;

            MountData.runningFrameStart = 0;
            MountData.runningFrameCount = 4;
            MountData.runningFrameDelay = 6;

            MountData.inAirFrameStart = 0;
            MountData.inAirFrameCount = 1;
            MountData.inAirFrameDelay = 12;

            MountData.idleFrameStart = 0;
            MountData.idleFrameCount = 1;
            MountData.idleFrameDelay = 12;
            MountData.idleFrameLoop = true;

            MountData.swimFrameStart = 0;
            MountData.swimFrameCount = 1;
            MountData.swimFrameDelay = 12;

            if (!Main.dedServ)
            {
                // The hull retains the original four 76x48 walking frames.
                // The turret is drawn independently immediately below this
                // front layer so it can mirror with the player's aim.
                MountData.frontTexture = ModContent.Request<Texture2D>(
                    "everflow/Content/Mounts/AlloyTank02_Hull");
                MountData.textureWidth = MountData.frontTexture.Width();
                MountData.textureHeight = MountData.frontTexture.Height();
            }
        }

        public override void SetMount(Player player, ref bool skipDust)
        {
            AlloyTank02Player tankPlayer = player.GetModPlayer<AlloyTank02Player>();

            if (tankPlayer.CrewCount <= 0)
            {
                int freeSlots = (int)System.Math.Floor(
                    player.maxMinions - player.slotsMinions + 0.001f);
                tankPlayer.CrewCount = System.Math.Clamp(freeSlots, 1, 3);
            }

            tankPlayer.HullDirection = player.direction == 0 ? 1 : player.direction;
        }

        public override void Dismount(Player player, ref bool skipDust)
        {
            player.GetModPlayer<AlloyTank02Player>().CrewCount = 0;
        }

        public override void UpdateEffects(Player player)
        {
            // 类似激光钻头平台：只要正在骑乘就禁止使用物品、武器和工具。
            player.noItems = true;
            player.statDefense += 5;

            int crewCount = player.GetModPlayer<AlloyTank02Player>().CrewCount;
            if (crewCount <= 0)
                return;

            player.maxMinions = System.Math.Max(0, player.maxMinions - crewCount);
            player.GetDamage(DamageClass.Ranged) += crewCount * 0.05f;
            player.GetDamage(DamageClass.Summon) += crewCount * 0.05f;
        }

        public override bool Draw(
            List<DrawData> playerDrawData,
            int drawType,
            Player drawPlayer,
            ref Texture2D texture,
            ref Texture2D glowTexture,
            ref Vector2 drawPosition,
            ref Rectangle frame,
            ref Color drawColor,
            ref Color glowColor,
            ref float rotation,
            ref SpriteEffects spriteEffects,
            ref Vector2 drawOrigin,
            ref float drawScale,
            float shadow)
        {
            // Terraria.Mount.Draw 会把普通坐骑的帧高统一减去2像素。
            // 本贴图每帧严格为76x48，必须恢复完整帧，否则最底部两行履带会被裁掉。
            frame.X = 0;
            frame.Width = 76;
            frame.Y = drawPlayer.mount._frame * 48;
            frame.Height = 48;

            // The separated hull is drawn by the vanilla mount front layer,
            // while the turret and driver use the visual-feet anchor. Bring
            // the hull onto that same anchor so it does not appear one player
            // offset down and one turret pivot to the side.
            // This correction is a fixed anchor conversion, not a facing
            // offset. Giving it player.direction caused the hull to jump to
            // the opposite side whenever the mouse crossed the player.
            drawPosition.X -= WeaponPivotOffset * TankDrawScale;
            drawPosition.Y -= 4f + drawPlayer.mount.PlayerOffset;

            int hullDirection = drawPlayer
                .GetModPlayer<AlloyTank02Player>()
                .HullDirection;
            if (hullDirection < 0)
                spriteEffects |= SpriteEffects.FlipHorizontally;
            else
            {
                spriteEffects &= ~SpriteEffects.FlipHorizontally;
                // The asymmetric hull sheet has a visual center roughly two
                // tiles to the right when facing right. Left-facing alignment
                // is already correct, so compensate only this orientation.
                drawPosition.X -= 32f;
            }

            // 2.25倍属于非整数缩放，保持坐标落在整数像素上。
            drawPosition.X = (float)System.Math.Round(drawPosition.X);
            drawPosition.Y = (float)System.Math.Round(drawPosition.Y);
            drawScale *= TankDrawScale;
            return true;
        }
    }
}
