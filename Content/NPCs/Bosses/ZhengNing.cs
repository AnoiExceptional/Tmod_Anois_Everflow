using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using everflow.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.NPCs.Bosses
{
    [AutoloadBossHead]
    public sealed class ZhengNing : ModNPC
    {
        private const int GroundedState = 0;
        private const int JumpingState = 1;
        private const int BaseJumpDelay = 300;
        private const int GemRainInterval = 20;
        private const int WindupTime = 30;
        private const float BaseJumpSpeed = 13f;
        private const float MaximumHorizontalJumpSpeed = 8f;
        private const float AirSteeringAcceleration = 0.16f;
        private const float MaximumAirSpeed = 8.5f;
        private const float EstimatedNpcGravity = 0.3f;
        private const float MaximumJumpHeight = 120f * 16f;
        private const float TailTurnDuration = 60f;
        internal const int JadeCount = 5;
        internal const int BaseContactDamage = 60;
        internal const float TailLength = 20f * 16f;

        private ref float State => ref NPC.ai[0];
        private ref float StateTimer => ref NPC.ai[1];
        private float tailFacingProgress;
        private bool tailFacingInitialized;

        private float TailFacingProgress => tailFacingProgress;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 2;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NeverDropsResourcePickups[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 176;
            NPC.height = 184;
            NPC.damage = BaseContactDamage;
            NPC.defense = 30;
            // 与经典模式世纪之花相同；专家/大师模式继续由原版难度缩放。
            NPC.lifeMax = 30000;
            NPC.knockBackResist = 0f;
            NPC.dontTakeDamage = true;

            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.netAlways = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = Item.buyPrice(gold: 15);
            NPC.aiStyle = -1;
            NPC.BossBar = ModContent.GetInstance<ZhengNingBossBar>();

            // 彩虹猫之刃射弹碰撞反弹时使用的猫叫音效。
            NPC.HitSound = SoundID.Item57;
            NPC.DeathSound = SoundID.NPCDeath14;
            Music = MusicID.Boss3;
        }

        private void EnsureJadesSpawned()
        {
            // 不在OnSpawn中嵌套生成附属NPC，避免生成钩子重入。
            // ai[2]在生成前就置位，确保任何情况下都只执行一次。
            if (Main.netMode == NetmodeID.MultiplayerClient || NPC.ai[2] != 0f)
                return;

            NPC.ai[2] = 1f;
            NPC.netUpdate = true;

            int[] jadeTypes =
            {
                ModContent.NPCType<ZhengNingNephrite>(),
                ModContent.NPCType<ZhengNingSerpentine>(),
                ModContent.NPCType<ZhengNingHeliotrope>(),
                ModContent.NPCType<ZhengNingDarkNephrite>(),
                ModContent.NPCType<ZhengNingAshJade>()
            };

            for (int i = 0; i < JadeCount; i++)
            {
                int jadeIndex = NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)NPC.Center.X,
                    (int)NPC.Center.Y,
                    jadeTypes[i],
                    ai0: NPC.whoAmI,
                    ai1: i);
                if (jadeIndex >= 0 && jadeIndex < Main.maxNPCs)
                    Main.npc[jadeIndex].netUpdate = true;
            }
        }

        public override void SetBestiary(
            BestiaryDatabase database,
            BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement(
                "Mods.everflow.Bestiary.ZhengNing"));
        }

        public override void AI()
        {
            EnsureJadesSpawned();

            if (Main.netMode != NetmodeID.MultiplayerClient &&
                !AnyLivingPlayer())
            {
                DespawnAfterPartyWipe();
                return;
            }

            if (!NPC.HasValidTarget)
                NPC.TargetClosest(faceTarget: false);

            if (!NPC.HasValidTarget)
            {
                NPC.velocity.X *= 0.94f;
                NPC.EncourageDespawn(60);
                return;
            }

            Player target = Main.player[NPC.target];
            FaceTarget(target);
            UpdateTailFacing();
            NPC.dontTakeDamage = CountLivingJades(NPC.whoAmI) > 0;
            if (!NPC.dontTakeDamage)
                DoSecondPhaseGemRain(target);

            if (State == GroundedState)
                DoGroundedAI(target);
            else
                DoJumpingAI(target);
        }

        private static bool AnyLivingPlayer()
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                    return true;
            }
            return false;
        }

        private void DespawnAfterPartyWipe()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC jade = Main.npc[i];
                if (!jade.active || jade.ModNPC is not ZhengNingJadeBase ||
                    (int)jade.ai[0] != NPC.whoAmI)
                {
                    continue;
                }

                jade.active = false;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.SyncNPC, number: i);
            }

            NPC.active = false;
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
        }

        internal static int CountLivingJades(int parentIndex)
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.ModNPC is ZhengNingJadeBase &&
                    (int)npc.ai[0] == parentIndex)
                    count++;
            }
            return count;
        }

        internal static void GetJadeLife(int parentIndex, out float life, out float lifeMax)
        {
            life = 0f;
            lifeMax = 0f;
            float scaledPartLifeMax = 0f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.ModNPC is not ZhengNingJadeBase ||
                    (int)npc.ai[0] != parentIndex)
                    continue;

                life += System.Math.Max(0, npc.life);
                scaledPartLifeMax = npc.lifeMax;
            }

            // 以仍存活部位的难度缩放后生命计算总护盾；全部击碎时归零。
            lifeMax = scaledPartLifeMax > 0f
                ? JadeCount * scaledPartLifeMax
                : JadeCount * ZhengNingJadeBase.BaseLife;
        }

        internal static Vector2 GetBodyDrawBottom(NPC parent) =>
            parent.Bottom + new Vector2(0f, parent.gfxOffY + 32f);

        internal static Vector2 GetTailAnchor(NPC parent)
        {
            // 尾根紧贴本体屁股：本体镜像时立即切换位置；仅尾身和尖端渐进转动。
            float anchorOffsetX = parent.spriteDirection == -1 ? 110f : -110f;
            return GetBodyDrawBottom(parent) + new Vector2(anchorOffsetX, -202f);
        }

        internal static Vector2 GetJadeCenter(NPC parent, int jadeSlot)
        {
            // 原始贴图面朝左：脸侧仅-40度一根，正上方一根，屁股侧40/80/120度三根。
            // 不交换玉石帧；本体镜像时整套尾巴逆时针转80度，
            // 使屁股侧仍有三根、正上方一根、头部侧一根。
            float mirroredRotation = MathHelper.Lerp(
                0f,
                -80f,
                GetTailFacingProgress(parent));
            float angleOffset = MathHelper.ToRadians(
                (jadeSlot - 1) * 40f + mirroredRotation);
            Vector2 direction = -Vector2.UnitY.RotatedBy(angleOffset);
            float hoverPhase = MathHelper.TwoPi * jadeSlot / JadeCount;
            float hover = (float)System.Math.Sin(
                Main.GameUpdateCount * MathHelper.TwoPi / 120f + hoverPhase) * 6f;
            return GetTailAnchor(parent) + direction * TailLength + Vector2.UnitY * hover;
        }

        private void FaceTarget(Player target)
        {
            int direction = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.direction = direction;
            NPC.spriteDirection = direction;
        }

        private void UpdateTailFacing()
        {
            float targetProgress = NPC.spriteDirection == 1 ? 1f : 0f;
            if (!tailFacingInitialized)
            {
                tailFacingProgress = targetProgress;
                tailFacingInitialized = true;
                return;
            }

            tailFacingProgress = MathHelper.Clamp(
                tailFacingProgress + System.Math.Sign(targetProgress - tailFacingProgress) / TailTurnDuration,
                0f,
                1f);
        }

        private static float GetTailFacingProgress(NPC parent)
        {
            if (parent.ModNPC is ZhengNing zhengNing && zhengNing.tailFacingInitialized)
                return zhengNing.TailFacingProgress;

            return parent.spriteDirection == 1 ? 1f : 0f;
        }

        private void DoGroundedAI(Player target)
        {
            NPC.velocity.X *= 0.8f;

            // 只有确实站在地面时才累计下一次重压，避免台阶边缘误起跳。
            if (NPC.velocity.Y != 0f)
                return;

            float lifeRatio = NPC.life / (float)NPC.lifeMax;
            StateTimer++;

            if (StateTimer < GetCurrentJumpDelay())
                return;

            State = JumpingState;
            StateTimer = 0f;

            // 以玩家头顶为目标顶点，按NPC默认重力反推起跳速度；最高追高120格。
            float lifeBasedMinimumJumpSpeed = BaseJumpSpeed + (1f - lifeRatio) * 3f;
            float desiredRise = MathHelper.Clamp(
                NPC.Bottom.Y - target.Top.Y,
                0f,
                MaximumJumpHeight);
            float heightBasedJumpSpeed = System.MathF.Sqrt(
                2f * EstimatedNpcGravity * desiredRise);
            float maximumJumpSpeed = System.MathF.Sqrt(
                2f * EstimatedNpcGravity * MaximumJumpHeight);
            float jumpSpeed = MathHelper.Clamp(
                System.Math.Max(lifeBasedMinimumJumpSpeed, heightBasedJumpSpeed),
                lifeBasedMinimumJumpSpeed,
                maximumJumpSpeed);

            // 按预计到达顶点的时间预测玩家横坐标，使高跳时仍会尝试落向玩家。
            float timeToApex = System.Math.Max(1f, jumpSpeed / EstimatedNpcGravity);
            float predictedTargetX = target.Center.X + target.velocity.X * timeToApex;
            float horizontalSpeed = MathHelper.Clamp(
                (predictedTargetX - NPC.Center.X) / timeToApex,
                -MaximumHorizontalJumpSpeed,
                MaximumHorizontalJumpSpeed);
            if (System.Math.Abs(horizontalSpeed) < 3f)
                horizontalSpeed = 3f * NPC.direction;

            NPC.velocity = new Vector2(horizontalSpeed, -jumpSpeed);
            NPC.netUpdate = true;
        }

        private float GetCurrentJumpDelay()
        {
            // 一阶段以及二阶段满血时保持原本的300帧间隔；本体生命越低，
            // 跳跃频率越高，濒死时达到2倍频率（150帧间隔）。
            if (NPC.dontTakeDamage)
                return BaseJumpDelay;

            float lifeRatio = MathHelper.Clamp(
                NPC.life / (float)NPC.lifeMax,
                0f,
                1f);
            float frequencyMultiplier = MathHelper.Lerp(
                2f,
                1f,
                lifeRatio);
            return BaseJumpDelay / frequencyMultiplier;
        }

        private void DoSecondPhaseGemRain(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            NPC.localAI[0]++;
            if (NPC.localAI[0] < GemRainInterval)
                return;

            NPC.localAI[0] = 0f;
            int gemType = Main.rand.Next(
                ZhengNingFallingGemProjectile.GemTypeCount);
            float speed = gemType ==
                ZhengNingFallingGemProjectile.Diamond ? 25f : 15f;
            float maximumAngle = gemType ==
                ZhengNingFallingGemProjectile.Agate ? 15f : 5f;
            Vector2 velocity = Vector2.UnitY.RotatedBy(
                MathHelper.ToRadians(Main.rand.NextFloat(
                    -maximumAngle,
                    maximumAngle))) * speed;
            float spawnX = gemType ==
                ZhengNingFallingGemProjectile.Diamond
                ? target.Center.X
                : target.Center.X + Main.rand.NextFloat(-880f, 880f);
            Vector2 spawnPosition = new(
                spawnX,
                target.Center.Y - Main.rand.NextFloat(650f, 800f));
            int damage = NPC.GetAttackDamage_ForProjectiles(
                BaseContactDamage * 0.5f,
                BaseContactDamage * 0.5f);

            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<ZhengNingFallingGemProjectile>(),
                damage,
                4f,
                Main.myPlayer,
                gemType,
                NPC.whoAmI);
        }

        private void DoJumpingAI(Player target)
        {
            StateTimer++;

            int desiredDirection = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.velocity.X += desiredDirection * AirSteeringAcceleration;
            NPC.velocity.X = MathHelper.Clamp(
                NPC.velocity.X,
                -MaximumAirSpeed,
                MaximumAirSpeed);

            // 至少离地数帧后再检测落地，防止起跳首帧被当作一次着陆。
            if (StateTimer <= 6f || NPC.velocity.Y != 0f)
                return;

            CreateStompEffect();
            SpawnLandingGemBurst(target);
            State = GroundedState;
            StateTimer = 0f;
            NPC.velocity.X *= 0.35f;
            NPC.netUpdate = true;
        }

        private void SpawnLandingGemBurst(Player target)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int gemCount = Main.rand.Next(5, 8);
            int damage = NPC.GetAttackDamage_ForProjectiles(
                BaseContactDamage * 0.5f,
                BaseContactDamage * 0.5f);
            int firstSide = Main.rand.NextBool() ? 1 : -1;

            for (int i = 0; i < gemCount; i++)
            {
                int gemType = Main.rand.Next(
                    ZhengNingFallingGemProjectile.GemTypeCount);
                int side = i % 2 == 0 ? firstSide : -firstSide;
                float speed = gemType ==
                    ZhengNingFallingGemProjectile.Diamond ? 25f : 15f;
                float upwardAngle = Main.rand.NextFloat(
                    MathHelper.ToRadians(28f),
                    MathHelper.ToRadians(68f));
                Vector2 velocity = new Vector2(
                    side * System.MathF.Cos(upwardAngle),
                    -System.MathF.Sin(upwardAngle)) * speed;
                Vector2 spawnPosition = NPC.Bottom + new Vector2(
                    side * Main.rand.NextFloat(18f, 54f),
                    -42f);

                if (gemType == ZhengNingFallingGemProjectile.Diamond)
                {
                    velocity = spawnPosition.DirectionTo(target.Center) *
                        speed;
                }

                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<
                        ZhengNingFallingGemProjectile>(),
                    damage,
                    4f,
                    Main.myPlayer,
                    gemType,
                    NPC.whoAmI,
                    2f);
            }
        }

        private void CreateStompEffect()
        {
            SoundEngine.PlaySound(SoundID.Item14, NPC.Bottom);

            for (int i = 0; i < 32; i++)
            {
                Vector2 position = new Vector2(
                    Main.rand.NextFloat(NPC.Left.X - 24f, NPC.Right.X + 24f),
                    NPC.Bottom.Y - Main.rand.NextFloat(2f, 10f));
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.Stone,
                    new Vector2(
                        Main.rand.NextFloat(-4.5f, 4.5f),
                        Main.rand.NextFloat(-5.5f, -1.2f)),
                    80,
                    default,
                    Main.rand.NextFloat(1.1f, 1.8f));
                dust.noGravity = false;
            }

            if (!Main.dedServ)
            {
                Main.LocalPlayer.GetModPlayer<ZhengNingScreenShakePlayer>()
                    .ApplyShake(NPC.Center, 900f, 7f);
            }
        }

        public override void FindFrame(int frameHeight)
        {
            bool windingUp = State == GroundedState &&
                StateTimer >= GetCurrentJumpDelay() - WindupTime;
            bool fallingFast = State == JumpingState && NPC.velocity.Y > 5f;
            NPC.frame.Y = (windingUp || fallingFast) ? frameHeight : 0;
        }

        public override bool PreDraw(
            SpriteBatch spriteBatch,
            Vector2 screenPos,
            Color drawColor)
        {
            DrawTailsAndJades(spriteBatch, screenPos, drawColor);

            Texture2D texture = TextureAssets.Npc[Type].Value;
            int frameHeight = texture.Height / Main.npcFrameCount[Type];
            Rectangle source = new Rectangle(
                0,
                NPC.frame.Y,
                texture.Width,
                frameHeight);

            // 原贴图面部朝左：玩家在左侧时不翻转，在右侧时水平镜像。
            SpriteEffects effects = NPC.spriteDirection == 1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            spriteBatch.Draw(
                texture,
                NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 32f),
                source,
                NPC.GetAlpha(drawColor),
                NPC.rotation,
                new Vector2(source.Width * 0.5f, source.Height),
                NPC.scale,
                effects,
                0f);
            return false;
        }

        private void DrawTailsAndJades(
            SpriteBatch spriteBatch,
            Vector2 screenPos,
            Color drawColor)
        {
            Texture2D chainTexture = ModContent.Request<Texture2D>(
                "everflow/Content/NPCs/Bosses/ZhengNing_Tail_1").Value;
            Texture2D tailEndTexture = ModContent.Request<Texture2D>(
                "everflow/Content/NPCs/Bosses/ZhengNing_Tail_2").Value;
            Texture2D jadeTexture = ModContent.Request<Texture2D>(
                "everflow/Content/NPCs/Bosses/ZhengNing_Jades").Value;
            Vector2 anchor = GetTailAnchor(NPC);

            for (int slot = 0; slot < JadeCount; slot++)
            {
                NPC jade = FindJade(NPC.whoAmI, slot);
                if (jade == null)
                    continue;

                Vector2 end = jade.Center;
                Vector2 chainVector = end - anchor;
                float chainLength = chainVector.Length();
                if (float.IsFinite(chainLength) && chainLength > 0.01f)
                {
                    // 根部和末端不动，仅将曲线控制点下压，形成受重力影响的松弛下弧。
                    float sag = MathHelper.Clamp(chainLength * 0.16f, 36f, 88f);
                    Vector2 control = (anchor + end) * 0.5f + Vector2.UnitY * sag;

                    // 体节数按20格基础长度固定。曲线和悬浮造成的长度变化只改变
                    // 体节间距/重叠量，不再在运动过程中增减体节。
                    float step = chainTexture.Height - 8f;
                    int segmentCount = (int)System.Math.Ceiling(TailLength / step);
                    for (int segment = 0; segment < segmentCount; segment++)
                    {
                        float t = (segment + 0.5f) / segmentCount;
                        Vector2 position = QuadraticBezier(anchor, control, end, t);
                        Vector2 tangent = QuadraticBezierTangent(anchor, control, end, t);
                        float rotation = tangent.ToRotation() - MathHelper.PiOver2;

                        spriteBatch.Draw(
                            chainTexture,
                            position - screenPos,
                            null,
                            NPC.GetAlpha(drawColor),
                            rotation,
                            chainTexture.Size() * 0.5f,
                            NPC.scale,
                            SpriteEffects.None,
                            0f);
                    }

                    // 末尾体节的宽端朝向玉石，细端与普通锁链衔接。
                    const float tailEndT = 0.97f;
                    Vector2 tailEndPosition = QuadraticBezier(
                        anchor,
                        control,
                        end,
                        tailEndT);
                    Vector2 tailEndTangent = QuadraticBezierTangent(
                        anchor,
                        control,
                        end,
                        tailEndT);
                    spriteBatch.Draw(
                        tailEndTexture,
                        tailEndPosition - screenPos,
                        null,
                        NPC.GetAlpha(drawColor),
                        tailEndTangent.ToRotation() + MathHelper.PiOver2,
                        tailEndTexture.Size() * 0.5f,
                        NPC.scale,
                        SpriteEffects.None,
                        0f);
                }

                int jadeFrameHeight = jadeTexture.Height / JadeCount;
                Rectangle jadeFrame = new Rectangle(
                    0,
                    slot * jadeFrameHeight,
                    jadeTexture.Width,
                    jadeFrameHeight);
                spriteBatch.Draw(
                    jadeTexture,
                    end - screenPos,
                    jadeFrame,
                    jade.GetAlpha(drawColor),
                    0f,
                    jadeFrame.Size() * 0.5f,
                    jade.scale,
                    SpriteEffects.None,
                    0f);
            }
        }

        private static Vector2 QuadraticBezier(
            Vector2 start,
            Vector2 control,
            Vector2 end,
            float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * start +
                2f * inverse * t * control +
                t * t * end;
        }

        private static Vector2 QuadraticBezierTangent(
            Vector2 start,
            Vector2 control,
            Vector2 end,
            float t)
        {
            Vector2 tangent = 2f * (1f - t) * (control - start) +
                2f * t * (end - control);
            return tangent.SafeNormalize(Vector2.UnitY);
        }

        private static NPC FindJade(int parentIndex, int jadeSlot)
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.ModNPC is ZhengNingJadeBase &&
                    (int)npc.ai[0] == parentIndex && (int)npc.ai[1] == jadeSlot)
                    return npc;
            }
            return null;
        }
    }

    public sealed class ZhengNingScreenShakePlayer : ModPlayer
    {
        private float shakeStrength;

        internal void ApplyShake(Vector2 source, float range, float strength)
        {
            float distance = Vector2.Distance(Player.Center, source);
            if (distance >= range)
                return;

            shakeStrength = System.Math.Max(
                shakeStrength,
                strength * (1f - distance / range));
        }

        public override void ModifyScreenPosition()
        {
            if (shakeStrength <= 0.05f)
                return;

            Main.screenPosition += Main.rand.NextVector2Circular(
                shakeStrength,
                shakeStrength);
            shakeStrength *= 0.82f;
        }
    }
}
