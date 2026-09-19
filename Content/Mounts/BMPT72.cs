using System.Collections.Generic;
using System.Linq;
using everflow.Common.Players;
using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Mounts
{
    public sealed class BMPT72 : ModMount
    {
        public const int FrameWidth = 240;
        public const int FrameHeight = 144;
        public const float HullMirrorAxisX = 116.5f;
        public const float GroundY = 142f;
        public static readonly Vector2 WeaponPivotInFrame = new(103f, 42f);
        // 用户坐标以左下角为原点：(横向, 向上)=(76.5,104.5)。
        // 转换为XNA左上角原点后为(76.5, 144-104.5)=(76.5,39.5)，
        // 正好对应炮塔贴图上的黑色圆形轴心。
        public static readonly Vector2 RocketWeaponPivotInFrame = new(76.5f, 39.5f);
        public const float LeftWeaponPivotCorrectionX = 7f;
        public const float WeaponLength = 68f;
        public const float RocketWeaponLength = 50.5f;
        public const float MaximumSpeed = 7.84f;
        public const float ThirtyMphSpeed = 5.88f;

        public static Vector2 GetVisualFeetWorld(Player player) =>
            // PlayerOffset只负责把驾驶员移到车体内部；载具自身必须锚定在
            // 玩家碰撞箱底部。把PlayerOffset再次减到这里会令全部载具图层
            // 额外上浮102像素。
            player.Bottom + Vector2.UnitY * player.gfxOffY;

        public static Vector2 GetFrameTopLeftWorld(Player player) =>
            GetVisualFeetWorld(player) - new Vector2(HullMirrorAxisX, GroundY);

        public static Vector2 GetWeaponPivotInFrame(int direction)
        {
            if (direction >= 0)
                return WeaponPivotInFrame;

            // 炮塔以x=116.5为中轴水平镜像，机炮连接点必须同步从
            // x=103镜像到x=130，否则朝左时炮管会留在原来的右向位置。
            return new Vector2(
                HullMirrorAxisX * 2f - WeaponPivotInFrame.X +
                    LeftWeaponPivotCorrectionX,
                WeaponPivotInFrame.Y);
        }

        public static Vector2 GetWeaponPivotWorld(Player player) =>
            GetFrameTopLeftWorld(player) + GetWeaponPivotInFrame(player.direction);

        public static Vector2 GetRocketWeaponPivotInFrame(int direction)
        {
            if (direction >= 0)
                return RocketWeaponPivotInFrame;

            return new Vector2(
                HullMirrorAxisX * 2f - RocketWeaponPivotInFrame.X,
                RocketWeaponPivotInFrame.Y);
        }

        public static Vector2 GetRocketWeaponPivotWorld(Player player) =>
            GetFrameTopLeftWorld(player) + GetRocketWeaponPivotInFrame(player.direction);

        public static Vector2 RoundScreenPosition(Vector2 worldPosition)
        {
            Vector2 result = worldPosition - Main.screenPosition;
            result.X = (float)System.Math.Round(result.X);
            result.Y = (float)System.Math.Round(result.Y);
            return result;
        }

        public override void SetStaticDefaults()
        {
            MountData.runSpeed = MaximumSpeed;
            MountData.dashSpeed = MaximumSpeed;
            MountData.acceleration = 0.20f;
            MountData.jumpHeight = 9;
            // jumpHeight是持续跳跃帧参数，不是格数。依据4速度实测约4格，
            // 按弹道高度与初速度平方的关系提高到6，以达到约9格离地高度。
            MountData.jumpSpeed = 6f;
            MountData.constantJump = false;
            MountData.blockExtraJumps = false;
            MountData.flightTimeMax = 0;
            MountData.fallDamage = 0.5f;

            MountData.buff = ModContent.BuffType<BMPT72Buff>();
            MountData.spawnDust = DustID.TintableDust;
            MountData.totalFrames = 4;
            // 驾驶员相较初版下移2.5格（40像素）。该值越小，人物显示越低。
            MountData.playerYOffsets = Enumerable.Repeat(62, 4).ToArray();
            MountData.xOffset = 0;
            MountData.yOffset = 0;
            MountData.playerHeadOffset = 62;
            // 原版玩家高42像素，额外增加38像素后正好达到5格（80像素）。
            MountData.heightBoost = 38;
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
                MountData.frontTexture = ModContent.Request<Texture2D>(
                    "everflow/Content/Mounts/BMPT_72_Hull");
                MountData.textureWidth = FrameWidth;
                MountData.textureHeight = FrameHeight * 4;
            }
        }

        public override void SetMount(Player player, ref bool skipDust)
        {
            BMPT72Player vehiclePlayer = player.GetModPlayer<BMPT72Player>();
            vehiclePlayer.ApplyVehicleHitbox();
            if (vehiclePlayer.CrewCount <= 0)
            {
                int freeSlots = (int)System.Math.Floor(
                    player.maxMinions - player.slotsMinions + 0.001f);
                vehiclePlayer.CrewCount = System.Math.Clamp(freeSlots, 1, 5);
            }

            vehiclePlayer.HullDirection = player.direction == 0 ? 1 : player.direction;
        }

        public override void Dismount(Player player, ref bool skipDust)
        {
            BMPT72Player vehiclePlayer = player.GetModPlayer<BMPT72Player>();
            vehiclePlayer.CrewCount = 0;
            vehiclePlayer.RestoreDefaultHitbox();
        }

        public override void UpdateEffects(Player player)
        {
            player.noItems = true;
            int crewCount = player.GetModPlayer<BMPT72Player>().CrewCount;
            player.statDefense += 50;
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
            frame = new Rectangle(0, drawPlayer.mount._frame * FrameHeight, FrameWidth, FrameHeight);
            drawPosition = RoundScreenPosition(GetVisualFeetWorld(drawPlayer));
            drawOrigin = new Vector2(HullMirrorAxisX, GroundY);
            drawScale = 1f;

            int direction = drawPlayer.GetModPlayer<BMPT72Player>().HullDirection;
            spriteEffects = direction < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            return true;
        }
    }
}
