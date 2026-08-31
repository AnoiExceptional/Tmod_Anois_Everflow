using System.IO;
using Microsoft.Xna.Framework;
using everflow.Content.Gores;
using everflow.Content.NPCs;
using everflow.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.NPCs.Bosses
{
    [AutoloadBossHead]
    public sealed class AncientTroop : ModNPC
    {
        private const int IntendedFrameCount = 8;
        private const int FramesPerPhase = 4;
        private const float PursuitSpeed = 1.5f;
        private const float PursuitAcceleration = 0.0375f;
        private const float ClosePursuitDistance = 30f * 16f;
        private const float ClosePursuitSpeedMultiplier = 0.5f;
        private const float ObstacleJumpSpeed = 7f;
        private const int FlameUseTime = 6;
        private const int FlameAttackDuration = 10 * 60;
        private const int FlameCooldownDuration = 15 * 60;
        private const float FlameSpeed = 8f;
        private const int FlameBaseDamage = 5;
        private const float FlameMuzzleOffset = 96f;
        private const float FlameMuzzleVerticalOffset = -16f;

        private const int FlameStateReady = 0;
        private const int FlameStateFiring = 1;
        private const int FlameStateCooldown = 2;

        private const int GrenadeUseTime = 5 * 60;
        private const float GrenadeEarHorizontalOffset = 44f;
        private const float GrenadeEarVerticalOffset = -121f;

        private int reinforcementWaves;
        private int reinforcementTimer;
        private int enginePulseTimer;
        private bool deathGoreSpawned;
        private bool phaseTransitionGoreSpawned;

        // Separate preset for Ancient Troop. It uses the same low-pitched
        // piston/drill source as Alloy Tank Mk. II without sharing state.
        internal static readonly SoundStyle TankEngineSFX_1 = SoundID.Item22 with
        {
            Volume = 1f,
            Pitch = -1f,
            PitchVariance = 0f,
            MaxInstances = 16
        };

        public override void SetStaticDefaults()
        {
            // Eight contiguous 192x160 frames: four for each phase.
            Main.npcFrameCount[Type] = IntendedFrameCount;
            NPCID.Sets.MPAllowedEnemies[Type] = true;
            NPCID.Sets.BossBestiaryPriority.Add(Type);
            NPCID.Sets.NeverDropsResourcePickups[Type] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 168;
            NPC.height = 96;
            NPC.damage = 35;
            NPC.defense = 10;
            NPC.lifeMax = 1500;
            NPC.knockBackResist = 0f;

            NPC.boss = true;
            NPC.npcSlots = 10f;
            NPC.netAlways = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = Item.buyPrice(gold: 5);

            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            Music = MusicID.Boss1;

            // A standalone pursuit AI prevents every projectile attack from
            // Santa-NK1's vanilla AI while preserving contact damage.
            NPC.aiStyle = -1;
        }

        public override void AI()
        {
            if (NPC.life <= NPC.lifeMax * 0.5f && !phaseTransitionGoreSpawned)
            {
                phaseTransitionGoreSpawned = true;
                SpawnPhaseTransitionGores();
            }

            HandleReinforcements();

            if (!NPC.HasValidTarget)
                NPC.TargetClosest(faceTarget: false);

            if (!NPC.HasValidTarget)
            {
                NPC.velocity.X *= 0.95f;
                NPC.EncourageDespawn(60);
                return;
            }

            Player target = Main.player[NPC.target];
            int targetDirection = target.Center.X >= NPC.Center.X ? 1 : -1;
            float movementMultiplier = GetMovementMultiplier();
            bool anchored = movementMultiplier <= 0f;

            if (!anchored)
            {
                NPC.direction = targetDirection;
                NPC.spriteDirection = targetDirection;
            }

            int attackDirection = NPC.direction == 0 ? targetDirection : NPC.direction;
            UpdateEngineSound(anchored);
            float horizontalDistance = System.Math.Abs(target.Center.X - NPC.Center.X);
            bool targetIsAhead = (target.Center.X - NPC.Center.X) * attackDirection >= 0f;
            float currentSpeed = PursuitSpeed * movementMultiplier;
            if (targetIsAhead && horizontalDistance <= ClosePursuitDistance)
                currentSpeed *= ClosePursuitSpeedMultiplier;

            float attackDelayMultiplier = GetAttackDelayMultiplier();
            int currentGrenadeUseTime = ScaleDelay(GrenadeUseTime, attackDelayMultiplier);
            int currentFlameUseTime = ScaleDelay(FlameUseTime, attackDelayMultiplier);
            int currentFlameCooldown = ScaleDelay(FlameCooldownDuration, attackDelayMultiplier);

            NPC.ai[3]++;
            if (NPC.ai[3] >= currentGrenadeUseTime)
            {
                NPC.ai[3] = 0f;
                Vector2 grenadePosition = NPC.Center + new Vector2(
                    attackDirection * GrenadeEarHorizontalOffset,
                    GrenadeEarVerticalOffset);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Item grenadeItem = new Item();
                    grenadeItem.SetDefaults(ItemID.Grenade);
                    Vector2 grenadeVelocity = Vector2.UnitX * attackDirection * grenadeItem.shootSpeed;

                    // Hostile projectiles apply Terraria's fixed conversion and
                    // world difficulty multiplier after spawning. Pass half of
                    // the intended normal-mode grenade damage to avoid scaling it twice.
                    int grenadeDamage = NPC.GetAttackDamage_ForProjectiles(
                        grenadeItem.damage * 0.5f,
                        grenadeItem.damage * 0.5f);

                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        grenadePosition,
                        grenadeVelocity,
                        ModContent.ProjectileType<AncientTroopGrenade>(),
                        grenadeDamage,
                        grenadeItem.knockBack,
                        Main.myPlayer);
                }

                if (Main.netMode != NetmodeID.Server)
                    SoundEngine.PlaySound(SoundID.Item61, grenadePosition);
            }

            Vector2 muzzlePosition = NPC.Center + new Vector2(
                attackDirection * FlameMuzzleOffset,
                FlameMuzzleVerticalOffset);
            bool targetInFlameRange = targetIsAhead &&
                Vector2.Distance(NPC.Center, target.Center) <= ClosePursuitDistance &&
                target.Center.Y >= muzzlePosition.Y;

            int flameState = (int)NPC.ai[0];
            if (flameState == FlameStateReady && targetInFlameRange)
            {
                NPC.ai[0] = FlameStateFiring;
                NPC.ai[1] = 0f;
                NPC.ai[2] = 0f;
                NPC.netUpdate = true;
                flameState = FlameStateFiring;
            }

            if (flameState == FlameStateFiring)
            {
                NPC.ai[1]++;
                NPC.ai[2]++;

                if (NPC.ai[2] >= currentFlameUseTime)
                {
                    NPC.ai[2] = 0f;
                    Vector2 flameDirection = (Vector2.UnitX * attackDirection)
                        .RotatedBy(attackDirection * MathHelper.ToRadians(30f));
                    Vector2 flameVelocity = flameDirection
                        .RotatedByRandom(MathHelper.ToRadians(5f)) * FlameSpeed;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(
                            NPC.GetSource_FromAI(),
                            muzzlePosition,
                            flameVelocity,
                            ModContent.ProjectileType<AncientTroopFlame>(),
                            FlameBaseDamage,
                            0f,
                            Main.myPlayer);
                    }

                    if (Main.netMode != NetmodeID.Server)
                        SoundEngine.PlaySound(SoundID.Item34, muzzlePosition);
                }

                if (NPC.ai[1] >= FlameAttackDuration)
                {
                    NPC.ai[0] = FlameStateCooldown;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.netUpdate = true;
                }
            }
            else if (flameState == FlameStateCooldown)
            {
                NPC.ai[1]++;
                if (NPC.ai[1] >= currentFlameCooldown)
                {
                    NPC.ai[0] = FlameStateReady;
                    NPC.ai[1] = 0f;
                    NPC.ai[2] = 0f;
                    NPC.netUpdate = true;
                }
            }

            // The 6 px/tick cap and 0.15 acceleration are 150% of the chosen
            // Santa-NK1-style pursuit baseline (4 and 0.1 respectively).
            // Cancel residual movement immediately when the target crosses
            // over the boss, so it never keeps driving away from its target.
            if (anchored)
            {
                NPC.velocity.X = 0f;
            }
            else if (NPC.velocity.X * targetDirection < 0f)
                NPC.velocity.X = targetDirection * PursuitAcceleration * movementMultiplier;
            else
                NPC.velocity.X = MathHelper.Clamp(
                    NPC.velocity.X + targetDirection * PursuitAcceleration * movementMultiplier,
                    -currentSpeed,
                    currentSpeed);

            // Keep advancing toward the target instead of turning around when
            // terrain blocks the tank. No projectile or auxiliary attack is
            // spawned here; NPC.damage therefore remains contact-only damage.
            if (!anchored && NPC.collideX && NPC.velocity.Y == 0f)
                NPC.velocity.Y = -ObstacleJumpSpeed;
        }

        private void HandleReinforcements()
        {
            if (NPC.life > NPC.lifeMax * 0.5f ||
                Main.netMode == NetmodeID.MultiplayerClient ||
                reinforcementWaves >= GetMaximumWaves())
            {
                return;
            }

            if (reinforcementWaves == 0)
            {
                SpawnReinforcementWave();
                return;
            }

            reinforcementTimer++;
            if (reinforcementTimer >= GetReinforcementInterval())
                SpawnReinforcementWave();
        }

        private void SpawnPhaseTransitionGores()
        {
            if (Main.dedServ)
                return;

            Vector2 inheritedVelocity = NPC.velocity * 0.35f;
            int gore02 = Gore.NewGore(
                NPC.GetSource_FromAI(),
                NPC.Center + new Vector2(-22f, -8f),
                inheritedVelocity + new Vector2(-2.4f, -3.2f),
                ModContent.GoreType<AncientTroop_Gore02>(),
                NPC.scale);
            int gore03 = Gore.NewGore(
                NPC.GetSource_FromAI(),
                NPC.Center + new Vector2(22f, -8f),
                inheritedVelocity + new Vector2(2.4f, -3.2f),
                ModContent.GoreType<AncientTroop_Gore03>(),
                NPC.scale);

            if (gore02 >= 0 && gore02 < Main.maxGore)
                Main.gore[gore02].rotation = -0.12f * NPC.spriteDirection;
            if (gore03 >= 0 && gore03 < Main.maxGore)
                Main.gore[gore03].rotation = 0.12f * NPC.spriteDirection;
        }

        private void UpdateEngineSound(bool anchored)
        {
            if (Main.dedServ)
                return;

            if (enginePulseTimer > 0)
            {
                enginePulseTimer--;
                return;
            }

            float engineVolume;
            if (anchored)
            {
                engineVolume = 0.10f;
            }
            else
            {
                float speedRatio = MathHelper.Clamp(
                    System.Math.Abs(NPC.velocity.X) / PursuitSpeed,
                    0f,
                    1f);
                engineVolume = MathHelper.Lerp(0.20f, 0.50f, speedRatio);
            }

            SoundEngine.PlaySound(
                TankEngineSFX_1 with { Volume = engineVolume },
                NPC.Center);

            // Reuse Alloy Tank Mk. II's established cadence: 12 frames while
            // driving and the 50%-slower 18-frame idle cadence when anchored.
            enginePulseTimer = anchored ? 17 : 11;
        }

        private void SpawnReinforcementWave()
        {
            reinforcementTimer = 0;
            int soldiersPerType = Main.expertMode && !Main.masterMode ? 3 : 2;
            Vector2 spawnCenter = NPC.Center + new Vector2(0f, 24f);

            for (int i = 0; i < soldiersPerType; i++)
            {
                float centeredIndex = i - (soldiersPerType - 1) * 0.5f;
                int meleeOffset = (int)(centeredIndex * 28f);
                int rangedOffset = (int)(centeredIndex * 28f +
                    (centeredIndex >= 0f ? 56f : -56f));

                NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)spawnCenter.X + meleeOffset,
                    (int)spawnCenter.Y,
                    ModContent.NPCType<AncientTrooper_Melee>());
                NPC.NewNPC(
                    NPC.GetSource_FromAI(),
                    (int)spawnCenter.X + rangedOffset,
                    (int)spawnCenter.Y,
                    ModContent.NPCType<AncientTrooper_Ranged>());
            }

            reinforcementWaves++;
            NPC.netUpdate = true;
        }

        private int GetMaximumWaves() => Main.masterMode ? 20 : Main.expertMode ? 5 : 4;

        private int GetReinforcementInterval() => Main.masterMode ? 150 : 300;

        private float GetMovementReductionPerWave() =>
            Main.masterMode ? 0.05f : Main.expertMode ? 0.20f : 0.25f;

        private float GetAttackDelayIncreasePerWave() =>
            Main.masterMode ? 0f : Main.expertMode ? 0.10f : 0.25f;

        private float GetMovementMultiplier() => MathHelper.Max(
            0f,
            1f - reinforcementWaves * GetMovementReductionPerWave());

        private float GetAttackDelayMultiplier() =>
            1f + reinforcementWaves * GetAttackDelayIncreasePerWave();

        private static int ScaleDelay(int baseDelay, float multiplier) =>
            System.Math.Max(1, (int)(baseDelay * multiplier + 0.5f));

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)reinforcementWaves);
            writer.Write((short)reinforcementTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            reinforcementWaves = reader.ReadByte();
            reinforcementTimer = reader.ReadInt16();
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            AddZeroToMaximumDrop(npcLoot, ItemID.CopperOre, 100);
            AddZeroToMaximumDrop(npcLoot, ItemID.TinOre, 100);
            AddZeroToMaximumDrop(npcLoot, ItemID.IronOre, 80);
            AddZeroToMaximumDrop(npcLoot, ItemID.LeadOre, 80);
            AddZeroToMaximumDrop(npcLoot, ItemID.SilverOre, 50);
            AddZeroToMaximumDrop(npcLoot, ItemID.TungstenOre, 50);
            AddZeroToMaximumDrop(npcLoot, ItemID.GoldOre, 20);
            AddZeroToMaximumDrop(npcLoot, ItemID.PlatinumOre, 20);
        }

        private static void AddZeroToMaximumDrop(NPCLoot npcLoot, int itemType, int maximum)
        {
            npcLoot.Add(ItemDropRule.Common(
                itemType,
                1,
                0,
                maximum));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || deathGoreSpawned || Main.dedServ)
                return;

            deathGoreSpawned = true;
            Vector2 goreVelocity = NPC.velocity * 0.35f +
                Main.rand.NextVector2Circular(2.5f, 2.5f) - Vector2.UnitY * 2f;
            int goreIndex = Gore.NewGore(
                NPC.GetSource_Death(),
                NPC.Center,
                goreVelocity,
                ModContent.GoreType<AncientTroop_Gore01>(),
                NPC.scale);

            if (goreIndex >= 0 && goreIndex < Main.maxGore)
                Main.gore[goreIndex].rotation = NPC.spriteDirection == -1 ? MathHelper.Pi : 0f;
        }

        public override void FindFrame(int frameHeight)
        {
            if (reinforcementWaves >= GetMaximumWaves())
            {
                NPC.frameCounter = 0d;
                NPC.frame.Y = FramesPerPhase * frameHeight;
                NPC.spriteDirection = NPC.direction;
                return;
            }

            int phaseStart = NPC.life <= NPC.lifeMax * 0.5f
                ? FramesPerPhase
                : 0;

            int currentFrame = NPC.frame.Y / frameHeight;
            if (currentFrame < phaseStart ||
                currentFrame >= phaseStart + FramesPerPhase ||
                currentFrame >= IntendedFrameCount)
            {
                currentFrame = phaseStart;
                NPC.frameCounter = 0d;
            }

            NPC.frameCounter++;
            if (NPC.frameCounter >= 7d)
            {
                NPC.frameCounter = 0d;
                currentFrame++;
                if (currentFrame >= phaseStart + FramesPerPhase)
                    currentFrame = phaseStart;
            }

            NPC.frame.Y = currentFrame * frameHeight;
            NPC.spriteDirection = NPC.direction;
        }

        public override void SetBestiary(
            BestiaryDatabase database,
            BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement(
                    "Mods.everflow.Bestiary.AncientTroop")
            });
        }
    }
}
