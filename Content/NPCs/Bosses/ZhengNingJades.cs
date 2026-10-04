using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Projectiles;

namespace everflow.Content.NPCs.Bosses
{
    public abstract class ZhengNingJadeBase : ModNPC
    {
        internal const int BaseLife = 3000;

        protected abstract int JadeSlot { get; }

        public override string Texture =>
            "everflow/Content/NPCs/Bosses/ZhengNing_Jades";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = ZhengNing.JadeCount;
            NPCID.Sets.NeverDropsResourcePickups[Type] = true;
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type,
                new NPCID.Sets.NPCBestiaryDrawModifiers { Hide = true });
        }

        public override void SetDefaults()
        {
            NPC.width = 54;
            NPC.height = 54;
            NPC.damage = 0;
            NPC.defense = 12;
            // 玉石是Boss附属部位而不是NPC.boss，不能依赖普通NPC的自动缩放。
            // 在属性初始化阶段直接按世界难度确定基础生命。
            int difficultyLifeMultiplier = Main.masterMode ? 3 : Main.expertMode ? 2 : 1;
            NPC.lifeMax = BaseLife * difficultyLifeMultiplier;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.netAlways = true;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.Tink;
            NPC.DeathSound = SoundID.Shatter;
        }

        public override void SetBestiary(
            BestiaryDatabase database,
            BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.UIInfoProvider = new CommonEnemyUICollectionInfoProvider(
                ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[Type],
                quickUnlock: true);
        }

        public override void AI()
        {
            int parentIndex = (int)NPC.ai[0];
            if (parentIndex < 0 || parentIndex >= Main.maxNPCs)
            {
                NPC.active = false;
                return;
            }

            NPC parent = Main.npc[parentIndex];
            if (!parent.active || parent.type != ModContent.NPCType<ZhengNing>())
            {
                NPC.active = false;
                return;
            }

            NPC.Center = ZhengNing.GetJadeCenter(parent, JadeSlot);
            NPC.velocity = Vector2.Zero;
            NPC.rotation = 0f;
            NPC.timeLeft = 750;
            NPC.target = parent.target;
            DoAttack(parent);
        }

        protected virtual void DoAttack(NPC parent)
        {
        }

        protected bool TryGetTarget(NPC parent, out Player target)
        {
            if (!parent.HasValidTarget)
                parent.TargetClosest(faceTarget: false);

            target = parent.HasValidTarget ? Main.player[parent.target] : null;
            return target != null && target.active && !target.dead;
        }

        protected int GetProjectileDamage(float baseDamage)
        {
            // 敌对射弹生成后还会应用原版难度伤害换算，因此传入一半基础值。
            return NPC.GetAttackDamage_ForProjectiles(baseDamage * 0.5f, baseDamage * 0.5f);
        }

        public override void FindFrame(int frameHeight)
        {
            NPC.frame.Y = JadeSlot * frameHeight;
        }

        public override bool PreDraw(
            Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch,
            Vector2 screenPos,
            Color drawColor) => false;

        public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

        public override void OnKill()
        {
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustDirect(
                    NPC.position,
                    NPC.width,
                    NPC.height,
                    DustID.GemDiamond,
                    Main.rand.NextFloat(-3f, 3f),
                    Main.rand.NextFloat(-3f, 3f),
                    80,
                    default,
                    Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
            }

            int parentIndex = (int)NPC.ai[0];
            if (parentIndex >= 0 && parentIndex < Main.maxNPCs &&
                Main.npc[parentIndex].active)
                Main.npc[parentIndex].netUpdate = true;
        }
    }

    public sealed class ZhengNingNephrite : ZhengNingJadeBase
    {
        protected override int JadeSlot => 0;

        protected override void DoAttack(NPC parent)
        {
            NPC.ai[2]++;
            if (NPC.ai[2] < 20f || !TryGetTarget(parent, out Player target))
                return;

            NPC.ai[2] = 0f;
            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(SoundID.Item5, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 velocity = NPC.DirectionTo(target.Center)
                .RotatedByRandom(MathHelper.ToRadians(3f)) * 20f;
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                velocity,
                ModContent.ProjectileType<JadeArrow>(),
                GetProjectileDamage(50f),
                3f,
                Main.myPlayer);
        }
    }

    public sealed class ZhengNingSerpentine : ZhengNingJadeBase
    {
        protected override int JadeSlot => 1;

        protected override void DoAttack(NPC parent)
        {
            NPC.ai[2]++;
            if (NPC.ai[2] < 180f)
                return;

            NPC.ai[2] = 0f;
            if (Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(SoundID.Item61, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int side = -1; side <= 1; side += 2)
            {
                for (int shot = 0; shot < 2; shot++)
                {
                    Vector2 direction = new Vector2(side, -0.28f - shot * 0.22f)
                        .SafeNormalize(Vector2.UnitX * side);
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        direction * 10f,
                        ModContent.ProjectileType<JadeRocket>(),
                        GetProjectileDamage(80f),
                        7f,
                        Main.myPlayer);
                }
            }
        }
    }

    public sealed class ZhengNingHeliotrope : ZhengNingJadeBase
    {
        protected override int JadeSlot => 2;

        protected override void DoAttack(NPC parent)
        {
            NPC.ai[2]++;
            if (NPC.localAI[0] <= 0f && NPC.ai[2] >= 120f)
            {
                NPC.ai[2] = 0f;
                NPC.localAI[0] = 3f;
                NPC.localAI[1] = 0f;
            }

            if (NPC.localAI[0] <= 0f)
                return;

            NPC.localAI[1]--;
            if (NPC.localAI[1] > 0f || !TryGetTarget(parent, out Player target))
                return;

            NPC.localAI[1] = 8f;
            NPC.localAI[0]--;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 velocity = NPC.DirectionTo(target.Center) * 12f;
            Projectile.NewProjectile(
                NPC.GetSource_FromAI(),
                NPC.Center,
                velocity,
                ProjectileID.DeathLaser,
                GetProjectileDamage(60f),
                0f,
                Main.myPlayer);
        }
    }

    public sealed class ZhengNingDarkNephrite : ZhengNingJadeBase
    {
        protected override int JadeSlot => 3;

        protected override void DoAttack(NPC parent)
        {
            NPC.ai[2]++;
            if (NPC.localAI[0] <= 0f && NPC.ai[2] >= 15f * 60f)
            {
                NPC.ai[2] = 0f;
                NPC.localAI[0] = 1f;
            }

            if (NPC.localAI[0] <= 0f)
                return;

            int elapsed = (int)NPC.localAI[0] - 1;
            if (elapsed >= 5 * 60)
            {
                NPC.localAI[0] = 0f;
                return;
            }

            if (elapsed % 20 == 0 && Main.netMode != NetmodeID.Server)
                SoundEngine.PlaySound(
                    SoundID.Item34 with { Volume = 0.7f, Pitch = -0.15f },
                    NPC.Center);

            // 降低生成密度，让四条火焰射流保持清晰轮廓而不是铺满屏幕。
            if (elapsed % 6 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float rotation = -MathHelper.Pi * elapsed / (5f * 60f);
                for (int directionIndex = 0; directionIndex < 4; directionIndex++)
                {
                    float angle = rotation + directionIndex * MathHelper.PiOver2;
                    Vector2 velocity = angle.ToRotationVector2() * 12f;
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        velocity,
                        ModContent.ProjectileType<JadeInkFlame>(),
                        GetProjectileDamage(25f),
                        1f,
                        Main.myPlayer);
                }
            }

            NPC.localAI[0]++;
        }
    }

    public sealed class ZhengNingAshJade : ZhengNingJadeBase
    {
        protected override int JadeSlot => 4;

        protected override void DoAttack(NPC parent)
        {
            NPC.ai[2]++;
            if (NPC.ai[2] >= 15f * 60f && Main.netMode != NetmodeID.Server)
            {
                float chargeProgress = MathHelper.Clamp(
                    (NPC.ai[2] - 15f * 60f) / (5f * 60f),
                    0f,
                    1f);
                CreateSiphonEffect(chargeProgress);
            }

            if (NPC.ai[2] < 20f * 60f)
                return;

            NPC.ai[2] = 0f;
            if (Main.netMode != NetmodeID.Server)
                CreateFlash();

            if (Main.netMode != NetmodeID.MultiplayerClient)
                EqualizeLivingJades((int)NPC.ai[0]);
        }

        private void CreateSiphonEffect(float progress)
        {
            Lighting.AddLight(
                NPC.Center,
                new Vector3(0.35f, 0.48f, 0.58f) * progress);

            int interval = System.Math.Max(1, 6 - (int)(progress * 5f));
            if ((int)NPC.ai[2] % interval != 0)
                return;

            int particleCount = 1 + (int)(progress * 3f);
            for (int i = 0; i < particleCount; i++)
            {
                float radius = MathHelper.Lerp(250f, 120f, progress) +
                    Main.rand.NextFloat(-24f, 24f);
                Vector2 spawnPosition = NPC.Center +
                    Main.rand.NextVector2CircularEdge(radius, radius);
                Vector2 velocity = spawnPosition.DirectionTo(NPC.Center) *
                    MathHelper.Lerp(2.5f, 7f, progress);
                Dust dust = Dust.NewDustPerfect(
                    spawnPosition,
                    DustID.GemDiamond,
                    velocity,
                    (int)MathHelper.Lerp(150f, 20f, progress),
                    Color.Lerp(new Color(95, 120, 135), Color.White, progress),
                    MathHelper.Lerp(0.8f, 1.6f, progress));
                dust.noGravity = true;
            }
        }

        private void CreateFlash()
        {
            SoundEngine.PlaySound(SoundID.Item4 with { Pitch = 0.35f }, NPC.Center);
            Lighting.AddLight(NPC.Center, new Vector3(1.8f, 2.1f, 2.4f));
            for (int i = 0; i < 100; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(12f, 12f) *
                    Main.rand.NextFloat(0.35f, 1f);
                Dust dust = Dust.NewDustPerfect(
                    NPC.Center,
                    DustID.GemDiamond,
                    velocity,
                    0,
                    Color.White,
                    Main.rand.NextFloat(1.2f, 2.2f));
                dust.noGravity = true;
            }
        }

        private static void EqualizeLivingJades(int parentIndex)
        {
            NPC[] living = new NPC[ZhengNing.JadeCount];
            int count = 0;
            int totalLife = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC jade = Main.npc[i];
                if (!jade.active || jade.ModNPC is not ZhengNingJadeBase ||
                    (int)jade.ai[0] != parentIndex)
                    continue;

                living[count++] = jade;
                totalLife += jade.life;
            }

            if (count <= 1)
                return;

            int sharedLife = totalLife / count;
            int remainder = totalLife % count;
            for (int i = 0; i < count; i++)
            {
                living[i].life = System.Math.Min(
                    living[i].lifeMax,
                    sharedLife + (i < remainder ? 1 : 0));
                living[i].netUpdate = true;
            }
        }
    }

    public sealed class ZhengNingBossBar : ModBossBar
    {
        public override Asset<Texture2D> GetIconTexture(ref Rectangle? iconFrame)
        {
            iconFrame = null;
            return ModContent.Request<Texture2D>(
                "everflow/Content/NPCs/Bosses/ZhengNing_Head_Boss");
        }

        public override bool? ModifyInfo(
            ref BigProgressBarInfo info,
            ref float life,
            ref float lifeMax,
            ref float shield,
            ref float shieldMax)
        {
            int npcIndex = info.npcIndexToAimAt;
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
                return false;

            NPC boss = Main.npc[npcIndex];
            if (!boss.active || boss.type != ModContent.NPCType<ZhengNing>())
                return false;

            life = boss.life;
            lifeMax = boss.lifeMax;
            ZhengNing.GetJadeLife(npcIndex, out shield, out shieldMax);
            return true;
        }
    }
}
