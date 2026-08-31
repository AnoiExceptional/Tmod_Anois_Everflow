using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using everflow.Content.Gores;
using everflow.Content.Players;
using everflow.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.NPCs.Bosses
{
    /// <summary>
    /// 睚眦头部。移动参数基于毁灭者并调整为24px/tick、0.1主加速度和0.075转向加速度，
    /// 但不调用原版AI，从根源上移除死亡激光、探测怪等毁灭者附加攻击。
    /// </summary>
    [AutoloadBossHead]
    public sealed class YaZi : ModNPC
    {
        private const string BossHeadPhaseOne =
            "everflow/Content/NPCs/Bosses/YaZi_Head_Boss_1";
        private const string BossHeadPhaseOneHalf =
            "everflow/Content/NPCs/Bosses/YaZi_Head_Boss_2";
        private const string BossHeadPhaseTwo =
            "everflow/Content/NPCs/Bosses/YaZi_Head_Boss_3";
        internal const float EyeCenterUpOffset = 12f;
        internal const float PhaseOneHalfThreshold = 0.60f;
        internal const float PhaseTwoThreshold = 0.25f;
        private const float MoveSpeed = 24f;
        private const float Acceleration = 0.12f;
        // 原版毁灭者的转向加速度为0.15；睚眦的地面最大转向能力为其120%。
        private const float TurnAcceleration = 0.18f;
        private const float AirGravity = 0.15f;
        private const float InitialPostPassGravity = 0.253125f;
        private const float PostPassGravityIncrease = 0.005f;
        private const float MaximumPostPassGravity = 0.9f;
        private const float ChargeState = 1f;
        private const float AimState = 2f;
        private const float BladeRevealState = 3f;
        private const float GreatHatredRetreatState = 4f;
        private const float PhaseTwoDisengageState = 5f;
        private const float AimCruiseSpeed = 6f;
        private const float PhaseTwoTurnSpeed = 1f;
        private const float GreatHatredRetreatDistance = 70f * 16f;
        private const float PhaseTwoDisengageDistance = 70f * 16f;
        private const int GreatHatredMinimumRetreatTime = 30;
        private const int GreatHatredMaximumRetreatTime = 300;
        private const int PhaseTwoMinimumDisengageTime = 20;
        private const int PhaseTwoMaximumDisengageTime = 300;
        private const int MinimumAimTime = 20;
        private const int MaximumChargeTime = 240;
        private const float ChargeAlignment = 0.985f;
        private const float ChargeSlashTriggerDistance = 20f * 16f;
        private const int ChargeSlashDuration = 60;
        private const float ChargeSlashHeadWobble =
            MathHelper.Pi / 36f;
        private bool armorPlateGoreSpawned;
        private float currentPostPassGravity = InitialPostPassGravity;
        private bool chargeSlashSelected;
        private bool chargeSlashTriggered;
        private int chargeSlashVisualTimer;
        private bool phaseTwoDarknessStarted;
        private bool phaseTwoDarknessReleased;

        internal bool PhaseTwoDarknessActive =>
            phaseTwoDarknessStarted && !phaseTwoDarknessReleased;

        private static readonly int[] SegmentPattern =
        {
            // (身2-身1-身2-身3) * 5
            2, 1, 2, 3,
            2, 1, 2, 3,
            2, 1, 2, 3,
            2, 1, 2, 3,
            2, 1, 2, 3,
            // 身2-身2-尾1-尾2
            2, 2, 4, 5
        };

        public override string Texture =>
            "everflow/Content/NPCs/Bosses/YaZi_Head_1";

        public override string BossHeadTexture =>
            BossHeadPhaseOne;

        public override void Load()
        {
            // 第一阶段头像由AutoloadBossHead注册；额外注册1.5与二阶段头像槽位。
            Mod.AddBossHeadTexture(BossHeadPhaseOneHalf);
            Mod.AddBossHeadTexture(BossHeadPhaseTwo);
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
        }

        public override void SetDefaults()
        {
            // 直接取得当前版本毁灭者的生命、伤害、防御、价值和难度缩放基准。
            NPC.CloneDefaults(NPCID.TheDestroyer);
            NPC.lifeMax = 50000;
            NPC.damage = 50;
            NPC.aiStyle = -1;
            NPC.width = 52;
            NPC.height = 52;
            NPC.alpha = 0;
            NPC.scale = 2f;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.netAlways = true;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit2 with
            {
                Pitch = -0.35f,
                PitchVariance = 0.05f
            };
            ApplyDebuffImmunities(NPC);
            Music = MusicID.OtherworldlyBoss1;
        }

        internal static void ApplyDebuffImmunities(NPC npc)
        {
            // 免疫全部原版及模组Debuff，仅允许灵液和流血。
            for (int buffType = 0; buffType < BuffLoader.BuffCount; buffType++)
                npc.buffImmune[buffType] = true;

            npc.buffImmune[BuffID.Ichor] = false;
            npc.buffImmune[BuffID.Bleeding] = false;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement(
                "Mods.everflow.Bestiary.YaZi"));
        }

        public override void OnSpawn(IEntitySource source)
        {
            currentPostPassGravity = InitialPostPassGravity;
            NPC.TargetClosest(faceTarget: false);
            if (!NPC.HasValidTarget)
                return;

            Player target = Main.player[NPC.target];
            // 以1920px作为标准屏幕宽度下限，避免服务器端较小的默认窗口值
            // 让Boss生成得过近；最终水平距离至少为两个屏幕宽度。
            float minimumSpawnDistance = System.Math.Max(Main.screenWidth, 1920) * 2f;
            float worldLeft = 320f;
            float worldRight = Main.maxTilesX * 16f - 320f;
            float leftRoom = target.Center.X - worldLeft;
            float rightRoom = worldRight - target.Center.X;

            bool canSpawnLeft = leftRoom >= minimumSpawnDistance;
            bool canSpawnRight = rightRoom >= minimumSpawnDistance;
            int spawnSide;
            if (canSpawnLeft && canSpawnRight)
                spawnSide = Main.rand.NextBool() ? -1 : 1;
            else if (canSpawnLeft)
                spawnSide = -1;
            else
                spawnSide = 1;

            float spawnX = target.Center.X + spawnSide * minimumSpawnDistance;
            spawnX = MathHelper.Clamp(spawnX, worldLeft, worldRight);
            float spawnY = MathHelper.Clamp(
                target.Center.Y + 320f,
                320f,
                Main.maxTilesY * 16f - 320f);

            NPC.Center = new Vector2(spawnX, spawnY);
            NPC.velocity = Vector2.Zero;
            NPC.netUpdate = true;
        }

        public override void BossHeadSlot(ref int index)
        {
            if (PhaseTwoDarknessActive)
            {
                index = -1;
                return;
            }

            float lifeRatio = NPC.life / (float)NPC.lifeMax;
            string bossHeadPath = lifeRatio <= PhaseTwoThreshold
                ? BossHeadPhaseTwo
                : lifeRatio <= PhaseOneHalfThreshold
                    ? BossHeadPhaseOneHalf
                    : BossHeadPhaseOne;
            index = ModContent.GetModBossHeadSlot(bossHeadPath);
        }

        public override void OnKill()
        {
            // Boss被击败后，所有玩家身上的“睚眦必报”立即失效。
            YaZiRetributionPlayer.ClearAllPlayers();
        }

        public override bool PreDraw(
            SpriteBatch spriteBatch,
            Vector2 screenPos,
            Color drawColor)
        {
            float lifeRatio = NPC.life / (float)NPC.lifeMax;
            bool phaseTwo = lifeRatio <= PhaseTwoThreshold;
            string texturePath = phaseTwo
                ? "everflow/Content/NPCs/Bosses/YaZi_Head_3"
                : lifeRatio <= PhaseOneHalfThreshold
                    ? "everflow/Content/NPCs/Bosses/YaZi_Head_2"
                    : Texture;
            Texture2D texture = ModContent.Request<Texture2D>(texturePath).Value;
            Rectangle frame = texture.Frame();

            // 剑先于头部绘制，使巨牙自然覆盖剑柄。
            YaZiSword.DrawHeldSword(NPC, screenPos, drawColor);

            if (phaseTwo)
            {
                // 眼部由头部在同一次绘制中先行绘制，保证它永远处于头部下层，
                // 且视觉位置不受独立NPC更新顺序或多人同步延迟影响。
                Vector2 headForward =
                    (NPC.rotation + MathHelper.PiOver2).ToRotationVector2();
                Vector2 eyeCenter = NPC.Center - headForward * EyeCenterUpOffset;
                float eyeRotation = NPC.rotation;
                if (NPC.target >= 0 &&
                    NPC.target < Main.maxPlayers &&
                    Main.player[NPC.target].active &&
                    !Main.player[NPC.target].dead)
                {
                    Vector2 toTarget = Main.player[NPC.target].Center - eyeCenter;
                    if (toTarget.LengthSquared() > 0.001f)
                        eyeRotation = toTarget.ToRotation() - MathHelper.PiOver2;
                }

                // 埋入实心图块时不绘制自发光层，避免光亮隔着物块可见。
                if (!Collision.SolidCollision(
                    eyeCenter - new Vector2(4f, 4f),
                    8,
                    8))
                {
                    Texture2D eyeTexture = ModContent.Request<Texture2D>(
                        "everflow/Content/NPCs/Bosses/YaZi_Eye").Value;
                    Rectangle eyeFrame = eyeTexture.Frame();
                    // 使用与环境亮度无关的中等强度自发光颜色，但不调用Lighting.AddLight，
                    // 因而不会照亮世界。眼部先画、头部后画，头部实心像素会完整遮住它。
                    Color eyeGlowColor = NPC.GetAlpha(new Color(190, 190, 190, 255));
                    Main.EntitySpriteDraw(
                        eyeTexture,
                        eyeCenter - screenPos + Vector2.UnitY * NPC.gfxOffY,
                        eyeFrame,
                        eyeGlowColor,
                        eyeRotation,
                        eyeFrame.Size() * 0.5f,
                        NPC.scale,
                        SpriteEffects.None,
                        0f);
                }
            }

            Main.EntitySpriteDraw(
                texture,
                NPC.Center - screenPos + Vector2.UnitY * NPC.gfxOffY,
                frame,
                NPC.GetAlpha(drawColor),
                NPC.rotation,
                frame.Size() * 0.5f,
                NPC.scale,
                SpriteEffects.None,
                0f);

            return false;
        }

        public override void AI()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.ai[2] == 0f)
                SpawnSegments();

            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.localAI[2] == 0f)
                SpawnHeldSword();

            if (!armorPlateGoreSpawned &&
                NPC.life <= NPC.lifeMax * PhaseOneHalfThreshold)
            {
                armorPlateGoreSpawned = true;
                YaZiRetributionPlayer.ClearAllPlayers();
                SpawnArmorPlateGore();
            }

            if (!phaseTwoDarknessStarted &&
                NPC.life <= NPC.lifeMax * PhaseTwoThreshold)
            {
                BeginGreatHatredMustBeAvenged();
            }

            if (Main.netMode != NetmodeID.MultiplayerClient &&
                NPC.life <= NPC.lifeMax * PhaseTwoThreshold &&
                NPC.localAI[1] == 0f)
            {
                SpawnPhaseTwoEye();
            }

            if (!NPC.HasValidTarget)
                NPC.TargetClosest(faceTarget: false);

            if (!NPC.HasValidTarget)
            {
                NPC.EncourageDespawn(60);
                NPC.velocity.Y = MathHelper.Clamp(
                    NPC.velocity.Y + AirGravity,
                    -MoveSpeed * 2f,
                    MoveSpeed * 2f);
                UpdateRotation();
                return;
            }

            Player target = Main.player[NPC.target];
            // 睚眦不受昼夜影响；只有当前仇恨目标死亡时才向地下撤离。
            bool fleeing = target.dead;

            if (fleeing)
            {
                NPC.EncourageDespawn(60);
                Vector2 fleeVelocity = Vector2.UnitY * MoveSpeed * 2f;
                TurnDirectlyTowards(
                    fleeVelocity.SafeNormalize(Vector2.UnitY),
                    MoveSpeed * 2f);
                UpdateRotation();
                return;
            }

            // 冲撞会主动离开玩家视野，不能让原版的远距离自然清理计时
            // 把仍在正常战斗循环中的头部判定为脱战。
            NPC.timeLeft = 1800;

            bool insideTerrain = IsInsideTerrain();
            if (NPC.ai[2] == BladeRevealState)
                UpdateBladeRevealState(target);
            else if (NPC.ai[2] == GreatHatredRetreatState)
                UpdateGreatHatredRetreatState(target);
            else if (NPC.ai[2] == PhaseTwoDisengageState)
                UpdatePhaseTwoDisengageState(target);
            else if (NPC.ai[2] == ChargeState)
                UpdateChargeState(target, insideTerrain);
            else
                UpdateAimState(target);

            UpdateRotation();
            ApplyChargeSlashHeadWobble();

            // 方向或钻地状态变化时主动同步，减少多人模式下长体节链的抖动。
            float terrainState = insideTerrain ? 1f : 0f;
            if (NPC.localAI[0] != terrainState)
            {
                NPC.localAI[0] = terrainState;
                NPC.netUpdate = true;
            }
        }

        private void UpdateChargeState(Player target, bool insideTerrain)
        {
            NPC.ai[1]++;
            // 二阶段完全替换普通冲撞：当前尚未越过玩家的冲撞以及此后
            // 每一轮冲撞都必定携带“冲锋斩击”。
            if (NPC.life <= NPC.lifeMax * PhaseTwoThreshold &&
                NPC.localAI[3] == 0f)
            {
                chargeSlashSelected = true;
            }
            Vector2 currentDirection = NPC.velocity.SafeNormalize(
                (target.Center - NPC.Center).SafeNormalize(Vector2.UnitY));

            if (chargeSlashSelected &&
                !chargeSlashTriggered &&
                NPC.localAI[3] == 0f &&
                Vector2.Distance(NPC.Center, target.Center) <=
                    ChargeSlashTriggerDistance)
            {
                TriggerChargeSlash();
            }

            // 已经越过玩家后不再追踪目标：保持冲撞惯性并受重力下坠，
            // 直到头部重新接触物块才立刻切换到下一轮转向瞄准。
            if (NPC.localAI[3] > 0f)
            {
                // 二阶段解除毁灭者式的触地限制：不受重力影响，也无需接触
                // 物块，越过玩家后立刻在空中以完整转向能力准备下一次冲撞。
                if (NPC.life <= NPC.lifeMax * PhaseTwoThreshold)
                {
                    BeginPhaseTwoDisengage();
                    return;
                }

                if (NPC.life > NPC.lifeMax * PhaseTwoThreshold)
                {
                    NPC.velocity.Y = MathHelper.Clamp(
                        NPC.velocity.Y + currentPostPassGravity,
                        -MoveSpeed,
                        MoveSpeed * 1.25f);
                    currentPostPassGravity = System.Math.Min(
                        currentPostPassGravity + PostPassGravityIncrease,
                        MaximumPostPassGravity);
                }

                if (insideTerrain)
                {
                    // 触地后立即卸掉大部分冲撞动量，避免以高速画出巨大回转圆。
                    // 保留当前运动方向，但将速度压到整备巡航速度，随后立刻索敌转向。
                    float impactSpeed = NPC.velocity.Length();
                    float recoverySpeed = impactSpeed > AimCruiseSpeed
                        ? MathHelper.Lerp(impactSpeed, AimCruiseSpeed, 0.5f)
                        : impactSpeed;
                    NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) *
                        recoverySpeed;
                    NPC.ai[2] = AimState;
                    NPC.ai[1] = 0f;
                    NPC.localAI[3] = 0f;
                    currentPostPassGravity = InitialPostPassGravity;
                    NPC.netUpdate = true;
                }
                return;
            }

            // 一阶段与1.5阶段在进入冲撞时锁死方向。二阶段则在越过玩家前
            // 保留正常最大转向能力的1/5，用于轻微修正高速冲锋轨迹。
            Vector2 chargeDirection = currentDirection;
            if (NPC.life <= NPC.lifeMax * PhaseTwoThreshold)
            {
                Vector2 desiredChargeDirection =
                    (target.Center - NPC.Center).SafeNormalize(chargeDirection);
                float currentAngle = chargeDirection.ToRotation();
                float desiredAngle = desiredChargeDirection.ToRotation();
                float angleDifference = MathHelper.WrapAngle(
                    desiredAngle - currentAngle);
                float maximumChargeTurn =
                    TurnAcceleration / AimCruiseSpeed * 0.2f;
                chargeDirection = (currentAngle + MathHelper.Clamp(
                    angleDifference,
                    -maximumChargeTurn,
                    maximumChargeTurn)).ToRotationVector2();
            }

            float chargeSpeed = Approach(
                NPC.velocity.Length(),
                MoveSpeed,
                Acceleration);
            // 冲撞阶段锁定方向，只沿当前直线持续加速，不再空中缠斗式追踪。
            NPC.velocity = chargeDirection * chargeSpeed;

            Vector2 toPlayer = target.Center - NPC.Center;
            bool playerHasBeenPassed =
                Vector2.Dot(toPlayer, chargeDirection) < 0f;
            if (playerHasBeenPassed)
            {
                // 即使冲刺轨迹没有进入20格主动触发半径，越过玩家的一刻也
                // 必须完成“冲锋斩击”；“大恨当雪”不会因为擦身而过落空。
                if (chargeSlashSelected && !chargeSlashTriggered)
                    TriggerChargeSlash();

                NPC.localAI[3] = 1f;
                if (chargeSlashTriggered &&
                    phaseTwoDarknessStarted &&
                    !phaseTwoDarknessReleased)
                {
                    CompleteGreatHatredMustBeAvenged();
                }
                if (NPC.life <= NPC.lifeMax * PhaseTwoThreshold)
                {
                    BeginPhaseTwoDisengage();
                    return;
                }
                if (NPC.life > NPC.lifeMax * PhaseTwoThreshold)
                {
                    NPC.velocity.Y = MathHelper.Clamp(
                        NPC.velocity.Y + currentPostPassGravity,
                        -MoveSpeed,
                        MoveSpeed * 1.25f);
                    currentPostPassGravity = System.Math.Min(
                        currentPostPassGravity + PostPassGravityIncrease,
                        MaximumPostPassGravity);
                }
            }

            // 只给尚未成功越过玩家的异常冲撞保留超时保险；正常冲撞必须
            // 等待下坠并接触地面，不再要求飞出屏幕。
            if (NPC.localAI[3] == 0f && NPC.ai[1] >= MaximumChargeTime)
            {
                NPC.ai[2] = AimState;
                NPC.ai[1] = 0f;
                NPC.netUpdate = true;
            }
        }

        /// <summary>
        /// 特殊攻击“大恨当雪”：二阶段转场压暗世界，并等待下一次
        /// “冲锋斩击”越过玩家后解除黑暗。
        /// </summary>
        private void BeginGreatHatredMustBeAvenged()
        {
            phaseTwoDarknessStarted = true;
            phaseTwoDarknessReleased = false;
            // “大恨当雪”会中断当前所有常规动作，先进入独立撤离整备阶段。
            NPC.ai[2] = GreatHatredRetreatState;
            NPC.ai[1] = 0f;
            NPC.localAI[3] = 0f;
            currentPostPassGravity = InitialPostPassGravity;
            chargeSlashSelected = true;
            chargeSlashTriggered = false;
            chargeSlashVisualTimer = 0;
            if (TryGetSwordProjectile(out Projectile sword) &&
                sword.ModProjectile is YaZiSword swordBehavior)
            {
                swordBehavior.CancelActiveMotion(NPC);
            }
            NPC.netUpdate = true;
        }

        private void UpdateGreatHatredRetreatState(Player target)
        {
            NPC.ai[1]++;
            Vector2 awayFromPlayer = (NPC.Center - target.Center)
                .SafeNormalize(-NPC.velocity.SafeNormalize(Vector2.UnitY));

            // 以强化的转向和加速度尽快脱离玩家附近，获得足够长的冲锋跑道。
            TurnDirectlyTowards(
                awayFromPlayer,
                MoveSpeed * 1.25f,
                turnMultiplier: 2.5f);

            float distance = Vector2.Distance(NPC.Center, target.Center);
            bool farEnough =
                distance >= GreatHatredRetreatDistance &&
                NPC.ai[1] >= GreatHatredMinimumRetreatTime;
            bool timedOut = NPC.ai[1] >= GreatHatredMaximumRetreatTime;
            if (!farEnough && !timedOut)
                return;

            // 撤离完成后才重新锁定玩家并进入标准的预判—加速冲锋流程。
            NPC.ai[2] = AimState;
            NPC.ai[1] = 0f;
            NPC.localAI[3] = 0f;
            chargeSlashSelected = true;
            chargeSlashTriggered = false;
            NPC.netUpdate = true;
        }

        private void BeginPhaseTwoDisengage()
        {
            NPC.ai[2] = PhaseTwoDisengageState;
            NPC.ai[1] = 0f;
            NPC.localAI[3] = 0f;
            currentPostPassGravity = InitialPostPassGravity;
            chargeSlashVisualTimer = 0;
            NPC.netUpdate = true;
        }

        private void UpdatePhaseTwoDisengageState(Player target)
        {
            NPC.ai[1]++;

            // BZ式脱离：越过目标后不立即掉头，沿现有航向保持高速直线飞离。
            Vector2 disengageDirection = NPC.velocity.SafeNormalize(
                (NPC.Center - target.Center).SafeNormalize(Vector2.UnitY));
            float disengageSpeed = Approach(
                NPC.velocity.Length(),
                MoveSpeed,
                Acceleration);
            NPC.velocity = disengageDirection * disengageSpeed;

            float distance = Vector2.Distance(NPC.Center, target.Center);
            bool farEnough =
                distance >= PhaseTwoDisengageDistance &&
                NPC.ai[1] >= PhaseTwoMinimumDisengageTime;
            bool timedOut = NPC.ai[1] >= PhaseTwoMaximumDisengageTime;
            if (!farEnough && !timedOut)
                return;

            // 拉开足够距离后才允许重新锁定。先在远处卸下高速惯性，
            // 否则以极速进入转向会自然形成围绕玩家的巨大圆周轨迹。
            NPC.velocity = NPC.velocity.SafeNormalize(Vector2.UnitY) *
                AimCruiseSpeed;
            NPC.ai[2] = AimState;
            NPC.ai[1] = 0f;
            NPC.localAI[3] = 0f;
            chargeSlashSelected = true;
            chargeSlashTriggered = false;
            chargeSlashVisualTimer = 0;
            NPC.netUpdate = true;
        }

        private void CompleteGreatHatredMustBeAvenged()
        {
            phaseTwoDarknessReleased = true;
            NPC.netUpdate = true;
        }

        private void TriggerChargeSlash()
        {
            chargeSlashTriggered = true;
            chargeSlashVisualTimer = ChargeSlashDuration;
            if (TryGetSwordProjectile(out Projectile sword) &&
                sword.ModProjectile is YaZiSword swordBehavior)
            {
                swordBehavior.BeginChargeSlash(NPC);
            }

            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(
                    SoundID.DD2_SonicBoomBladeSlash with
                    {
                        Volume = SoundID.DD2_SonicBoomBladeSlash.Volume * 1.5f
                    },
                    NPC.Center);
            NPC.netUpdate = true;
        }

        private void UpdateBladeRevealState(Player target)
        {
            if (!TryGetSwordProjectile(out Projectile sword))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    SpawnHeldSword();
                return;
            }

            Vector2 toHandle = sword.Center - NPC.Center;
            Vector2 direction = toHandle.SafeNormalize(Vector2.UnitY);
            float speed = Approach(
                NPC.velocity.Length(),
                MoveSpeed,
                Acceleration);
            // “锋芒乍现”期间始终锁定剑柄，加速直冲其当前位置。
            NPC.velocity = direction * speed;

            bool swordIsPlanted = sword.ai[1] == YaZiSword.PlantedState;
            float catchDistance = MouthForwardOffsetForReveal + 20f;
            if (!swordIsPlanted || toHandle.Length() > catchDistance)
                return;

            if (sword.ModProjectile is YaZiSword swordBehavior)
                swordBehavior.AttachToHead(NPC);

            NPC.ai[2] = AimState;
            NPC.ai[1] = 0f;
            NPC.localAI[3] = 0f;
            currentPostPassGravity = InitialPostPassGravity;
            NPC.netUpdate = true;
        }

        private const float MouthForwardOffsetForReveal = 28f;

        private bool TryGetSwordProjectile(out Projectile sword)
        {
            int swordType = ModContent.ProjectileType<YaZiSword>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile candidate = Main.projectile[i];
                if (candidate.active &&
                    candidate.type == swordType &&
                    (int)candidate.ai[0] == NPC.whoAmI)
                {
                    sword = candidate;
                    return true;
                }
            }

            sword = default;
            return false;
        }

        private void UpdateAimState(Player target)
        {
            NPC.ai[1]++;
            float distance = Vector2.Distance(NPC.Center, target.Center);
            float predictionFrames = MathHelper.Clamp(
                distance / MoveSpeed,
                15f,
                75f);
            Vector2 predictedTarget =
                target.Center + target.velocity * predictionFrames;
            Vector2 aimDirection =
                (predictedTarget - NPC.Center).SafeNormalize(Vector2.UnitY);

            // 不再使用毁灭者按X/Y轴分别修正、刻意制造宽回转圆的转向。
            // 睚眦在整备阶段直接减速，并始终沿最短角度转向预判点；
            // 一旦完成对齐便立刻进入下一次冲撞。
            // 只有二阶段使用远端低速紧凑掉头；此前阶段恢复原本的
            // 落地后巡航转向逻辑，不改变其重力、触地和回转手感。
            bool phaseTwo = NPC.life <= NPC.lifeMax * PhaseTwoThreshold;
            float turningSpeed = phaseTwo
                ? PhaseTwoTurnSpeed
                : AimCruiseSpeed;
            TurnDirectlyTowards(aimDirection, turningSpeed);

            Vector2 velocityDirection =
                NPC.velocity.SafeNormalize(aimDirection);
            bool aligned = Vector2.Dot(velocityDirection, aimDirection) >= ChargeAlignment;
            if (NPC.ai[1] >= MinimumAimTime &&
                aligned)
            {
                // 方向确认后进入直线加速冲撞；此后不再修正瞄准方向。
                NPC.velocity = aimDirection *
                    System.Math.Max(NPC.velocity.Length(), AimCruiseSpeed);
                NPC.ai[2] = ChargeState;
                NPC.ai[1] = 0f;
                NPC.localAI[3] = 0f;
                currentPostPassGravity = InitialPostPassGravity;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // 二阶段前每次冲撞有1/3概率转为“冲锋斩击”；
                    // 二阶段仍然必定使用。
                    chargeSlashSelected = phaseTwo || Main.rand.NextBool(3);
                    chargeSlashTriggered = false;
                    chargeSlashVisualTimer = 0;
                }
                NPC.netUpdate = true;
            }
        }

        private void ApplyChargeSlashHeadWobble()
        {
            if (chargeSlashVisualTimer <= 0)
                return;

            float progress = 1f -
                chargeSlashVisualTimer / (float)ChargeSlashDuration;
            NPC.rotation += System.MathF.Sin(progress * MathHelper.TwoPi * 2f) *
                ChargeSlashHeadWobble;
            chargeSlashVisualTimer--;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(chargeSlashSelected);
            writer.Write(chargeSlashTriggered);
            writer.Write((byte)chargeSlashVisualTimer);
            writer.Write(phaseTwoDarknessStarted);
            writer.Write(phaseTwoDarknessReleased);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            chargeSlashSelected = reader.ReadBoolean();
            chargeSlashTriggered = reader.ReadBoolean();
            chargeSlashVisualTimer = reader.ReadByte();
            phaseTwoDarknessStarted = reader.ReadBoolean();
            phaseTwoDarknessReleased = reader.ReadBoolean();
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            // 第一阶段头部拥有50%减伤；降至60%生命后进入1.5阶段并失去该减伤。
            if (NPC.life > NPC.lifeMax * PhaseOneHalfThreshold)
                modifiers.FinalDamage *= 0.50f;
        }

        private void SpawnSegments()
        {
            // 生成后先在屏幕外完成一次预判瞄准，再发动首次冲撞。
            NPC.ai[2] = AimState;
            NPC.ai[1] = 0f;
            NPC.localAI[3] = 0f;
            currentPostPassGravity = InitialPostPassGravity;
            NPC.ai[3] = NPC.whoAmI;
            NPC.realLife = NPC.whoAmI;

            int previous = NPC.whoAmI;
            foreach (int part in SegmentPattern)
            {
                int type = part switch
                {
                    1 => ModContent.NPCType<YaZi_Body1>(),
                    2 => ModContent.NPCType<YaZi_Body2>(),
                    3 => ModContent.NPCType<YaZi_Body3>(),
                    4 => ModContent.NPCType<YaZi_Tail1>(),
                    _ => ModContent.NPCType<YaZi_Tail2>()
                };

                int segment = NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)NPC.Center.X,
                    (int)NPC.Center.Y,
                    type,
                    0,
                    0f,
                    previous,
                    0f,
                    NPC.whoAmI);

                Main.npc[segment].realLife = NPC.whoAmI;
                Main.npc[segment].netUpdate = true;
                Main.npc[previous].ai[0] = segment;
                Main.npc[previous].netUpdate = true;
                previous = segment;

                NetMessage.SendData(MessageID.SyncNPC, number: segment);
            }

            NPC.netUpdate = true;
        }

        private void SpawnHeldSword()
        {
            NPC.localAI[2] = 1f;
            // 原版敌对射弹命中流程固定先乘2，tModLoader随后再应用世界
            // EnemyDamageMultiplier（普通1/专家2/大师3）。传入基础伤害的一半，
            // 最终便按正常世界难度得到约75/150/225点防御前伤害。
            int swordDamage = NPC.GetAttackDamage_ForProjectiles(
                YaZiSword.BaseDamage * 0.5f,
                YaZiSword.BaseDamage * 0.5f);
            Player target = NPC.HasValidTarget
                ? Main.player[NPC.target]
                : Main.player[Player.FindClosest(NPC.position, NPC.width, NPC.height)];
            float halfScreenHeight =
                System.Math.Max(Main.screenHeight, 1080) * 0.5f;
            Vector2 revealHandle = target.Center - Vector2.UnitY *
                (halfScreenHeight + YaZiSword.VisualBladeLength + 384f);
            int sword = Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                revealHandle,
                Vector2.Zero,
                ModContent.ProjectileType<YaZiSword>(),
                swordDamage,
                0f,
                Main.myPlayer,
                NPC.whoAmI,
                YaZiSword.FallingState);

            if (sword >= 0 && sword < Main.maxProjectiles)
                Main.projectile[sword].netUpdate = true;

            NPC.ai[2] = BladeRevealState;
            NPC.ai[1] = 0f;
            NPC.netUpdate = true;
        }

        private void SpawnPhaseTwoEye()
        {
            NPC.localAI[1] = 1f;
            int eye = NPC.NewNPC(
                NPC.GetSource_FromAI(),
                (int)NPC.Center.X,
                (int)NPC.Center.Y,
                ModContent.NPCType<YaZiEye>(),
                0,
                0f,
                0f,
                0f,
                NPC.whoAmI);

            Main.npc[eye].realLife = NPC.whoAmI;
            Main.npc[eye].netUpdate = true;
            NetMessage.SendData(MessageID.SyncNPC, number: eye);
        }

        private void SpawnArmorPlateGore()
        {
            if (Main.dedServ)
                return;

            Vector2 forward =
                (NPC.rotation + MathHelper.PiOver2).ToRotationVector2();
            Vector2 launchVelocity =
                NPC.velocity * 0.35f -
                forward * 2.5f +
                Main.rand.NextVector2Circular(1.5f, 1.5f);

            // Gore素材为28x40，2倍绘制时以其中心对齐头部中心。
            Vector2 spawnPosition = NPC.Center - new Vector2(28f, 40f);
            int goreIndex = Gore.NewGore(
                NPC.GetSource_FromAI(),
                spawnPosition,
                launchVelocity,
                ModContent.GoreType<YaZi_Gore01>(),
                NPC.scale);

            if (goreIndex >= 0 && goreIndex < Main.maxGore)
                Main.gore[goreIndex].rotation = NPC.rotation;
        }

        private bool IsInsideTerrain()
        {
            Rectangle hitbox = NPC.Hitbox;
            int minX = Utils.Clamp(hitbox.Left / 16 - 1, 1, Main.maxTilesX - 2);
            int maxX = Utils.Clamp(hitbox.Right / 16 + 1, 1, Main.maxTilesX - 2);
            int minY = Utils.Clamp(hitbox.Top / 16 - 1, 1, Main.maxTilesY - 2);
            int maxY = Utils.Clamp(hitbox.Bottom / 16 + 1, 1, Main.maxTilesY - 2);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    bool solid = WorldGen.SolidTile(x, y);
                    bool liquid = tile.LiquidAmount > 64;
                    if (!solid && !liquid)
                        continue;

                    if (hitbox.Intersects(new Rectangle(x * 16, y * 16, 16, 16)))
                        return true;
                }
            }

            return false;
        }

        private void TurnDirectlyTowards(
            Vector2 desiredDirection,
            float targetSpeed,
            float turnMultiplier = 1f)
        {
            desiredDirection = desiredDirection.SafeNormalize(Vector2.UnitY);
            float currentSpeed = NPC.velocity.Length();
            float speedChange = currentSpeed > targetSpeed
                ? Acceleration * 2f * turnMultiplier
                : Acceleration * turnMultiplier;
            currentSpeed = Approach(currentSpeed, targetSpeed, speedChange);

            if (NPC.velocity.LengthSquared() <= 0.001f)
            {
                NPC.velocity = desiredDirection * currentSpeed;
                return;
            }

            float currentAngle = NPC.velocity.ToRotation();
            float desiredAngle = desiredDirection.ToRotation();
            float angleDifference = MathHelper.WrapAngle(
                desiredAngle - currentAngle);
            float maximumTurn =
                TurnAcceleration / AimCruiseSpeed * turnMultiplier;
            float newAngle = currentAngle + MathHelper.Clamp(
                angleDifference,
                -maximumTurn,
                maximumTurn);
            NPC.velocity = newAngle.ToRotationVector2() * currentSpeed;
        }

        private void UpdateRotation()
        {
            if (NPC.velocity.LengthSquared() > 0.001f)
                // 睚眦原图的头部朝下；rotation=0 时正方向为 +Y。
                NPC.rotation = NPC.velocity.ToRotation() - MathHelper.PiOver2;
        }

        private static float Approach(float value, float target, float amount)
        {
            if (value < target)
                return System.Math.Min(value + amount, target);
            if (value > target)
                return System.Math.Max(value - amount, target);
            return value;
        }
    }

    /// <summary>
    /// 二阶段覆盖在头部上的特殊眼部体节。它是独立NPC，方便后续扩展攻击逻辑，
    /// 目前不承担碰撞和受击判定。
    /// </summary>
    public sealed class YaZiEye : ModNPC
    {
        public override string Texture =>
            "everflow/Content/NPCs/Bosses/YaZi_Eye";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.NeverDropsResourcePickups[Type] = true;
            NPCID.Sets.NPCBestiaryDrawOffset.Add(
                Type,
                new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
        }

        public override void SetDefaults()
        {
            NPC.width = 52;
            NPC.height = 52;
            NPC.damage = 0;
            NPC.defense = 0;
            NPC.lifeMax = 1;
            NPC.aiStyle = -1;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.dontTakeDamage = true;
            NPC.immortal = true;
            NPC.chaseable = false;
            NPC.netAlways = true;
            NPC.scale = 2f;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override bool PreDraw(
            SpriteBatch spriteBatch,
            Vector2 screenPos,
            Color drawColor)
        {
            // 可见眼部由头部PreDraw统一绘制，以确保严格绑定和正确层级。
            return false;
        }

        public override void AI()
        {
            int headIndex = (int)NPC.ai[3];
            if (headIndex < 0 ||
                headIndex >= Main.maxNPCs ||
                !Main.npc[headIndex].active ||
                Main.npc[headIndex].type != ModContent.NPCType<YaZi>())
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            NPC head = Main.npc[headIndex];
            NPC.realLife = headIndex;
            NPC.scale = head.scale;
            NPC.alpha = head.alpha;
            NPC.behindTiles = head.behindTiles;

            // 原图正方向朝下，因此-forward就是贴图局部坐标的“向上”。
            Vector2 headForward =
                (head.rotation + MathHelper.PiOver2).ToRotationVector2();
            NPC.Center = head.Center - headForward * YaZi.EyeCenterUpOffset;

            // 位置随头部运动，但眼部以自身中心为旋转轴，始终朝向头部的仇恨目标。
            if (head.target >= 0 &&
                head.target < Main.maxPlayers &&
                Main.player[head.target].active &&
                !Main.player[head.target].dead)
            {
                Vector2 toTarget = Main.player[head.target].Center - NPC.Center;
                if (toTarget.LengthSquared() > 0.001f)
                    NPC.rotation = toTarget.ToRotation() - MathHelper.PiOver2;
            }
            else
            {
                NPC.rotation = head.rotation;
            }

            NPC.velocity = Vector2.Zero;
            NPC.timeLeft = 10;
        }
    }

    public abstract class YaZiSegment : ModNPC
    {
        // 头部与全部身体/尾部贴图均为64x64，并以2倍绘制：
        // 从旋转中心到上沿或下沿中点的精确距离均为64世界像素。
        private const float HalfVisualLength = 64f;

        protected abstract int VanillaCloneType { get; }
        protected virtual bool ReceivesPhaseOneDamageReduction => true;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.NeverDropsResourcePickups[Type] = true;
            NPCID.Sets.RespawnEnemyID[Type] = ModContent.NPCType<YaZi>();
            NPCID.Sets.NPCBestiaryDrawOffset.Add(
                Type,
                new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(VanillaCloneType);
            NPC.lifeMax = 50000;
            NPC.damage = 35;
            NPC.aiStyle = -1;
            NPC.width = 44;
            NPC.height = 44;
            NPC.alpha = 0;
            NPC.scale = 2f;
            NPC.boss = false;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.netAlways = true;
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit2 with
            {
                Pitch = -0.35f,
                PitchVariance = 0.05f
            };
            YaZi.ApplyDebuffImmunities(NPC);
        }

        // 睚眦会在冲撞循环中主动远离玩家。禁止引擎按距离或非活跃时间
        // 自然清理体节；头部失效时仍由AI中的链条检查主动移除。
        public override bool CheckActive() => false;

        public override bool PreDraw(
            SpriteBatch spriteBatch,
            Vector2 screenPos,
            Color drawColor)
        {
            // 默认NPC绘制以碰撞箱底部校准；睚眦的44px碰撞箱与
            // 64px、2倍贴图不匹配。强制以NPC.Center为贴图中心，
            // 使AI计算的上下沿关节与实际绘制边缘完全一致。
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame();
            Main.EntitySpriteDraw(
                texture,
                NPC.Center - screenPos + Vector2.UnitY * NPC.gfxOffY,
                frame,
                NPC.GetAlpha(drawColor),
                NPC.rotation,
                frame.Size() * 0.5f,
                NPC.scale,
                SpriteEffects.None,
                0f);
            return false;
        }

        public override void ModifyIncomingHit(ref NPC.HitModifiers modifiers)
        {
            if (!ReceivesPhaseOneDamageReduction)
                return;

            int headIndex = (int)NPC.ai[3];
            if (IsValidHead(headIndex))
            {
                NPC head = Main.npc[headIndex];
                // 非尾部身体在第一阶段及1.5阶段保留30%减伤，二阶段解除。
                if (head.life > head.lifeMax * YaZi.PhaseTwoThreshold)
                    modifiers.FinalDamage *= 0.70f;
            }
        }

        public override void AI()
        {
            int previousIndex = (int)NPC.ai[1];
            int headIndex = (int)NPC.ai[3];
            if (!IsValidPart(previousIndex) || !IsValidHead(headIndex))
            {
                NPC.active = false;
                NPC.netUpdate = true;
                return;
            }

            NPC.realLife = headIndex;
            NPC targetPart = Main.npc[previousIndex];
            Vector2 previousForward =
                (targetPart.rotation + MathHelper.PiOver2).ToRotationVector2();
            // 上一张贴图的上沿中点，作为两节共享的关节世界坐标。
            Vector2 previousBackAnchor =
                targetPart.Center - previousForward * HalfVisualLength;
            Vector2 towardPrevious = previousBackAnchor - NPC.Center;
            if (towardPrevious.LengthSquared() < 0.001f)
                towardPrevious = previousForward;

            Vector2 direction = towardPrevious.SafeNormalize(Vector2.UnitY);
            NPC.rotation = direction.ToRotation() - MathHelper.PiOver2;
            // 当前贴图的下沿中点与上一节上沿中点完全重合；这个共享点也是关节旋转轴。
            NPC.Center = previousBackAnchor - direction * HalfVisualLength;
            NPC.velocity = Vector2.Zero;
            NPC.spriteDirection = 1;
            // 原版蠕虫体节在离玩家过远时会被自然清理。睚眦的冲撞会主动
            // 飞出屏幕，因此只要头部仍有效，就必须持续刷新整条体节链的寿命。
            NPC.timeLeft = 750;
        }

        private static bool IsValidPart(int index) =>
            index >= 0 &&
            index < Main.maxNPCs &&
            Main.npc[index].active &&
            Main.npc[index].life > 0;

        private static bool IsValidHead(int index) =>
            index >= 0 &&
            index < Main.maxNPCs &&
            Main.npc[index].active &&
            Main.npc[index].type == ModContent.NPCType<YaZi>() &&
            Main.npc[index].life > 0;
    }

    public sealed class YaZi_Body1 : YaZiSegment
    {
        protected override int VanillaCloneType => NPCID.TheDestroyerBody;
        public override string Texture => "everflow/Content/NPCs/Bosses/YaZi_Body_1";
    }

    public sealed class YaZi_Body2 : YaZiSegment
    {
        protected override int VanillaCloneType => NPCID.TheDestroyerBody;
        public override string Texture => "everflow/Content/NPCs/Bosses/YaZi_Body_2";
    }

    public sealed class YaZi_Body3 : YaZiSegment
    {
        protected override int VanillaCloneType => NPCID.TheDestroyerBody;
        public override string Texture => "everflow/Content/NPCs/Bosses/YaZi_Body_3";
    }

    public sealed class YaZi_Tail1 : YaZiSegment
    {
        protected override int VanillaCloneType => NPCID.TheDestroyerTail;
        protected override bool ReceivesPhaseOneDamageReduction => false;
        public override string Texture => "everflow/Content/NPCs/Bosses/YaZi_Tail_1";
    }

    public sealed class YaZi_Tail2 : YaZiSegment
    {
        protected override int VanillaCloneType => NPCID.TheDestroyerTail;
        protected override bool ReceivesPhaseOneDamageReduction => false;
        public override string Texture => "everflow/Content/NPCs/Bosses/YaZi_Tail_2";
    }
}
