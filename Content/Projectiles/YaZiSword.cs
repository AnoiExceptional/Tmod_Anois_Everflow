using everflow.Content.NPCs.Bosses;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class YaZiSword : ModProjectile
    {
        internal const int BaseDamage = 75;
        internal static readonly float VisualBladeLength =
            SourceBladeLength * SwordScale;
        internal const float FallingState = 1f;
        internal const float PlantedState = 2f;
        internal const float HeldState = 0f;
        private static readonly Vector2 TextureHandle = new(19f, 82f);
        private static readonly Vector2 TextureTip = new(82f, 12f);
        private static readonly float SourceBladeAngle =
            (TextureTip - TextureHandle).ToRotation();
        private static readonly float SourceBladeLength =
            Vector2.Distance(TextureHandle, TextureTip);

        private const float MouthForwardOffset = 28f;
        private const float SwordScale = 4f;
        private const int SideFlipDuration = 90;
        private const int SideFlipCooldownDuration = 180;
        private const float BelowSideThreshold = 0.05f;
        private const float FallingAcceleration = 0.45f;
        private const float MaximumFallingSpeed = 18f;
        private const int AttachAdjustmentDuration = 24;
        private const float PlantedHeightCorrection = 15f * 16f;
        private const int ChargeSlashDuration = 30;
        private const int ProtectedLifetime = 36000;

        private bool orientationInitialized;
        private bool currentSideIsOpposite;
        private int sideFlipTimer;
        private float sideFlipStartOffset;
        private float currentSideOffset;
        private int sideFlipCooldown;
        private int chargeSlashTimer;
        private float chargeSlashStartAngle;

        public override string Texture =>
            "everflow/Content/Projectiles/YaZi_Sword_1";

        public override void SetDefaults()
        {
            Projectile.width = 384;
            Projectile.height = 384;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            // 常驻Boss武器不能使用仅两帧的脆弱续命窗口；远距离同步偶尔
            // 跳过一帧AI时也必须继续存活，只由头部绑定失效主动Kill。
            Projectile.timeLeft = ProtectedLifetime;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            int headIndex = (int)Projectile.ai[0];
            if (!TryGetHead(headIndex, out NPC head))
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.ai[1] == FallingState)
            {
                Projectile.rotation = MathHelper.PiOver2 - SourceBladeAngle;
                Projectile.velocity.X = 0f;
                Projectile.velocity.Y = System.Math.Min(
                    Projectile.velocity.Y + FallingAcceleration,
                    MaximumFallingSpeed);

                Vector2 fallingHandle = Projectile.Center;
                // 将停止探针放到设计插入边界下方15格：探针碰地时剑仍在
                // 更高位置，从而自然停住，不再先让剑柄落地后再瞬移上移。
                Vector2 stopProbe = fallingHandle + Vector2.UnitY *
                    (VisualBladeLength * 0.75f + PlantedHeightCorrection);
                float sweepDistance = System.Math.Max(
                    Projectile.velocity.Y,
                    1f);
                Rectangle sweptProbe = new(
                    (int)stopProbe.X - 6,
                    (int)(stopProbe.Y - sweepDistance) - 6,
                    12,
                    (int)System.Math.Ceiling(sweepDistance) + 12);
                if (Collision.SolidCollision(
                    sweptProbe.TopLeft(),
                    sweptProbe.Width,
                    sweptProbe.Height))
                {
                    Projectile.ai[1] = PlantedState;
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                }

                Projectile.timeLeft = ProtectedLifetime;
                return;
            }

            if (Projectile.ai[1] == PlantedState)
            {
                Projectile.rotation = MathHelper.PiOver2 - SourceBladeAngle;
                Projectile.velocity = Vector2.Zero;
                Projectile.timeLeft = ProtectedLifetime;
                return;
            }

            UpdateBladeSide(head);
            GetSwordPose(
                head,
                Projectile.localAI[0],
                out Vector2 handle,
                out Vector2 bladeDirection,
                out float rotation);
            Projectile.Center = handle +
                bladeDirection * SourceBladeLength * SwordScale * 0.5f;
            Projectile.rotation = rotation;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = ProtectedLifetime;
        }

        public override bool CanHitPlayer(Player target) =>
            Projectile.ai[1] == HeldState ||
            Projectile.ai[1] == FallingState;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Projectile.ai[1] == FallingState)
            {
                // “锋芒乍现”下坠阶段的Center就是剑柄旋转轴；按实际绘制方向
                // 检测从剑柄到剑尖的整段剑刃。落地后的PlantedState不会进入此处。
                Vector2 fallingHandle = Projectile.Center;
                Vector2 fallingBladeDirection =
                    (Projectile.rotation + SourceBladeAngle).ToRotationVector2();
                Vector2 fallingTip = fallingHandle +
                    fallingBladeDirection * SourceBladeLength * SwordScale;
                float fallingCollisionPoint = 0f;
                return Collision.CheckAABBvLineCollision(
                    targetHitbox.TopLeft(),
                    targetHitbox.Size(),
                    fallingHandle,
                    fallingTip,
                    36f,
                    ref fallingCollisionPoint);
            }

            if (Projectile.ai[1] != HeldState)
                return false;

            int headIndex = (int)Projectile.ai[0];
            if (!TryGetHead(headIndex, out NPC head))
                return false;

            float bladeAngle = GetCurrentBladeAngle(head);
            GetSwordPose(
                head,
                bladeAngle,
                out Vector2 handle,
                out Vector2 bladeDirection,
                out _);
            Vector2 tip = handle + bladeDirection * SourceBladeLength * SwordScale;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                handle,
                tip,
                36f,
                ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[1] == HeldState)
                return false;

            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame();
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                lightColor,
                Projectile.rotation,
                TextureHandle,
                SwordScale,
                SpriteEffects.None,
                0f);
            return false;
        }

        internal static void DrawHeldSword(NPC head, Vector2 screenPos, Color drawColor)
        {
            if (!TryGetAttachedSwordAngle(head, out float bladeAngle))
                return;
            GetSwordPose(
                head,
                bladeAngle,
                out Vector2 handle,
                out Vector2 bladeDirection,
                out float rotation);
            bool phaseTwo =
                head.life <= head.lifeMax * YaZi.PhaseTwoThreshold;
            string swordTexturePath = phaseTwo
                ? "everflow/Content/Projectiles/YaZi_Sword_2"
                : "everflow/Content/Projectiles/YaZi_Sword_1";
            Texture2D texture = ModContent.Request<Texture2D>(
                swordTexturePath).Value;
            Rectangle frame = texture.Frame();
            Main.EntitySpriteDraw(
                texture,
                handle - screenPos + Vector2.UnitY * head.gfxOffY,
                frame,
                head.GetAlpha(drawColor),
                rotation,
                TextureHandle,
                SwordScale,
                SpriteEffects.None,
                0f);

            if (phaseTwo)
            {
                Vector2 bladeCenter = handle +
                    bladeDirection * SourceBladeLength * SwordScale * 0.5f;
                // 剑身主体埋入实心图块时关闭自发光层；普通剑身仍按原层级绘制。
                bool glowOccluded = Collision.SolidCollision(
                    bladeCenter - new Vector2(4f, 4f),
                    8,
                    8);
                if (!glowOccluded)
                {
                    Texture2D glowTexture = ModContent.Request<Texture2D>(
                        "everflow/Content/Projectiles/YaZi_Sword_Glow").Value;
                    Rectangle glowFrame = glowTexture.Frame();
                    // 固定暖色自发光，不调用Lighting.AddLight，因此不会照亮环境。
                    Color warmGlow = head.GetAlpha(new Color(255, 190, 120, 255));
                    Main.EntitySpriteDraw(
                        glowTexture,
                        handle - screenPos + Vector2.UnitY * head.gfxOffY,
                        glowFrame,
                        warmGlow,
                        rotation,
                        TextureHandle,
                        SwordScale,
                        SpriteEffects.None,
                        0f);
                }
            }
        }

        private void UpdateBladeSide(NPC head)
        {
            if (sideFlipCooldown > 0)
                sideFlipCooldown--;

            if (chargeSlashTimer > 0)
            {
                float progress = 1f -
                    chargeSlashTimer / (float)ChargeSlashDuration;
                float easedProgress = progress * progress * (3f - 2f * progress);
                Projectile.localAI[0] =
                    chargeSlashStartAngle + MathHelper.TwoPi * easedProgress;
                chargeSlashTimer--;
                if (chargeSlashTimer == 0)
                {
                    Projectile.localAI[0] = chargeSlashStartAngle;
                    sideFlipCooldown = SideFlipCooldownDuration;
                    Projectile.netUpdate = true;
                }
                return;
            }

            // ai[2]保存咬住剑后的角度校正剩余时间，避免客户端跳过校正与冷却。
            if (Projectile.ai[2] > 0f)
            {
                float targetAngle = GetUpperSideAngle(head);
                float progress = 1f -
                    Projectile.ai[2] / AttachAdjustmentDuration;
                progress = progress * progress * (3f - 2f * progress);
                Projectile.localAI[0] = MathHelper.PiOver2 +
                    MathHelper.WrapAngle(targetAngle - MathHelper.PiOver2) * progress;
                Projectile.ai[2]--;
                if (Projectile.ai[2] <= 0f)
                {
                    currentSideIsOpposite = false;
                    currentSideOffset = 0f;
                    orientationInitialized = true;
                    Projectile.localAI[0] = targetAngle;
                    sideFlipCooldown = SideFlipCooldownDuration;
                    Projectile.netUpdate = true;
                }
                return;
            }

            Vector2 headForward =
                (head.rotation + MathHelper.PiOver2).ToRotationVector2();
            Vector2 baseSide = headForward.RotatedBy(-MathHelper.PiOver2);

            if (!orientationInitialized)
            {
                orientationInitialized = true;
                currentSideIsOpposite = baseSide.Y > 0f;
                currentSideOffset = currentSideIsOpposite ? MathHelper.Pi : 0f;
                Projectile.localAI[1] = 1f;
            }

            if (sideFlipTimer == 0)
            {
                Vector2 currentSide = currentSideIsOpposite ? -baseSide : baseSide;
                if (sideFlipCooldown <= 0 && currentSide.Y > BelowSideThreshold)
                {
                    sideFlipTimer = 1;
                    sideFlipStartOffset = currentSideIsOpposite
                        ? MathHelper.Pi
                        : 0f;
                }
                else
                {
                    currentSideOffset = currentSideIsOpposite
                        ? MathHelper.Pi
                        : 0f;
                }
            }

            if (sideFlipTimer > 0)
            {
                float progress = sideFlipTimer / (float)SideFlipDuration;
                progress = MathHelper.Clamp(progress, 0f, 1f);
                float easedProgress = progress * progress * (3f - 2f * progress);

                // Terraria屏幕坐标中正角度为顺时针；固定增加PI完成180度翻转。
                currentSideOffset =
                    sideFlipStartOffset + MathHelper.Pi * easedProgress;
                sideFlipTimer++;

                if (sideFlipTimer > SideFlipDuration)
                {
                    currentSideIsOpposite = !currentSideIsOpposite;
                    currentSideOffset = currentSideIsOpposite
                        ? MathHelper.Pi
                        : 0f;
                    sideFlipTimer = 0;
                    sideFlipCooldown = SideFlipCooldownDuration;
                    Projectile.netUpdate = true;
                }
            }

            Projectile.localAI[0] = baseSide.ToRotation() + currentSideOffset;
        }

        private float GetCurrentBladeAngle(NPC head)
        {
            if (Projectile.localAI[1] != 0f)
                return Projectile.localAI[0];

            return GetUpperSideAngle(head);
        }

        internal void AttachToHead(NPC head)
        {
            sideFlipTimer = 0;
            orientationInitialized = true;
            Projectile.localAI[0] = MathHelper.PiOver2;
            Projectile.localAI[1] = 1f;
            Projectile.ai[1] = HeldState;
            Projectile.ai[2] = AttachAdjustmentDuration;
            sideFlipCooldown = SideFlipCooldownDuration;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
        }

        internal void BeginChargeSlash(NPC head)
        {
            if (Projectile.ai[1] != HeldState || chargeSlashTimer > 0)
                return;

            chargeSlashStartAngle = Projectile.localAI[1] != 0f
                ? Projectile.localAI[0]
                : GetUpperSideAngle(head);
            Projectile.localAI[0] = chargeSlashStartAngle;
            Projectile.localAI[1] = 1f;
            chargeSlashTimer = ChargeSlashDuration;
            sideFlipTimer = 0;
            sideFlipCooldown = SideFlipCooldownDuration;
            Projectile.netUpdate = true;
        }

        internal void CancelActiveMotion(NPC head)
        {
            chargeSlashTimer = 0;
            sideFlipTimer = 0;
            Projectile.ai[2] = 0f;
            orientationInitialized = true;
            currentSideIsOpposite = false;
            currentSideOffset = 0f;
            chargeSlashStartAngle = GetUpperSideAngle(head);
            Projectile.localAI[0] = chargeSlashStartAngle;
            Projectile.localAI[1] = 1f;
            sideFlipCooldown = SideFlipCooldownDuration;
            Projectile.netUpdate = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((short)sideFlipCooldown);
            writer.Write((byte)chargeSlashTimer);
            writer.Write(chargeSlashStartAngle);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            sideFlipCooldown = reader.ReadInt16();
            chargeSlashTimer = reader.ReadByte();
            chargeSlashStartAngle = reader.ReadSingle();
        }

        private static bool TryGetAttachedSwordAngle(NPC head, out float bladeAngle)
        {
            int swordType = ModContent.ProjectileType<YaZiSword>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active &&
                    projectile.type == swordType &&
                    (int)projectile.ai[0] == head.whoAmI &&
                    projectile.ai[1] == HeldState &&
                    projectile.localAI[1] != 0f)
                {
                    bladeAngle = projectile.localAI[0];
                    return true;
                }
            }

            bladeAngle = 0f;
            return false;
        }

        private static float GetUpperSideAngle(NPC head)
        {
            Vector2 headForward =
                (head.rotation + MathHelper.PiOver2).ToRotationVector2();
            Vector2 side = headForward.RotatedBy(-MathHelper.PiOver2);
            if (side.Y > 0f)
                side = -side;
            return side.ToRotation();
        }

        private static void GetSwordPose(
            NPC head,
            float bladeAngle,
            out Vector2 handle,
            out Vector2 bladeDirection,
            out float rotation)
        {
            Vector2 headForward =
                (head.rotation + MathHelper.PiOver2).ToRotationVector2();
            handle = head.Center + headForward * MouthForwardOffset;
            bladeDirection = bladeAngle.ToRotationVector2();
            rotation = bladeAngle - SourceBladeAngle;
        }

        private static bool TryGetHead(int index, out NPC head)
        {
            if (index >= 0 &&
                index < Main.maxNPCs &&
                Main.npc[index].active &&
                Main.npc[index].type == ModContent.NPCType<YaZi>())
            {
                head = Main.npc[index];
                return true;
            }

            head = default;
            return false;
        }
    }
}
