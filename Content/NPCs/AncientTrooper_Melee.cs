using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Projectiles;

namespace everflow.Content.NPCs
{
    public sealed class AncientTrooper_Melee : ModNPC
    {
        private const int FrameCount = 26;
        private const int WalkFirstFrame = 1;
        private const int WalkLastFrame = 14;
        private const int AttackFirstFrame = 21;
        private const int AttackFrameCount = 5;
        private const int AttackDuration = 17;
        private const int AttackCooldown = 20;
        private const int BaseAttackDamage = 10;
        private const float PreferredMeleeDistance = 36f;
        private const float MaximumMeleeDistance = 48f;
        private const float MoveSpeed = 2.5f;
        private const float MoveAcceleration = 0.1f;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            NPC.width = 18;
            NPC.height = 40;
            NPC.damage = 10;
            NPC.defense = 8;
            NPC.lifeMax = 200;
            NPC.knockBackResist = 0.45f;
            NPC.aiStyle = -1;
            NPC.hide = true;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = Item.buyPrice(silver: 5);
        }

        public override void DrawBehind(int index)
        {
            Main.instance.DrawCacheNPCProjectiles.Add(index);
        }

        public override void AI()
        {
            if (!NPC.HasValidTarget)
                NPC.TargetClosest(faceTarget: false);

            if (!NPC.HasValidTarget)
            {
                NPC.velocity.X *= 0.9f;
                NPC.EncourageDespawn(60);
                return;
            }

            Player target = Main.player[NPC.target];
            int direction = target.Center.X >= NPC.Center.X ? 1 : -1;
            NPC.direction = direction;
            NPC.spriteDirection = direction;

            bool standingOnGround = NPC.collideY ||
                Collision.SolidCollision(
                    new Vector2(NPC.position.X + 2f, NPC.Bottom.Y),
                    NPC.width - 4,
                    3);

            if (NPC.ai[1] > 0f)
            {
                NPC.ai[1]--;
                if (standingOnGround)
                    NPC.velocity.X *= 0.78f;
                return;
            }

            NPC.ai[0]++;
            float horizontalDistance = System.Math.Abs(target.Center.X - NPC.Center.X);
            float verticalDistance = System.Math.Abs(target.Center.Y - NPC.Center.Y);

            if (horizontalDistance > PreferredMeleeDistance)
            {
                NPC.velocity.X = MathHelper.Clamp(
                    NPC.velocity.X + direction * MoveAcceleration,
                    -MoveSpeed,
                    MoveSpeed);
            }
            else
            {
                NPC.velocity.X *= 0.72f;
            }

            int obstacleTileX = (int)((NPC.Center.X + direction * (NPC.width * 0.5f + 8f)) / 16f);
            int feetTileY = (int)(NPC.Bottom.Y / 16f);
            bool obstacleAhead = WorldGen.SolidTile(obstacleTileX, feetTileY - 1) ||
                WorldGen.SolidTile(obstacleTileX, feetTileY - 2);
            bool targetIsReachablyAbove = target.Center.Y <= NPC.Center.Y - 32f &&
                horizontalDistance <= 10f * 16f;

            if (standingOnGround && (NPC.collideX || obstacleAhead || targetIsReachablyAbove))
            {
                NPC.velocity.Y = -6.5f;
                NPC.netUpdate = true;
            }

            if (horizontalDistance <= MaximumMeleeDistance && verticalDistance <= 64f && NPC.ai[0] >= AttackCooldown)
            {
                NPC.ai[0] = 0f;
                NPC.ai[1] = AttackDuration;
                if (standingOnGround)
                    NPC.velocity.X *= 0.35f;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        NPC.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<AncientTrooperMeleeSlash>(),
                        NPC.GetAttackDamage_ForProjectiles(
                            BaseAttackDamage * 0.5f,
                            BaseAttackDamage * 0.5f),
                        4f,
                        Main.myPlayer,
                        NPC.whoAmI);
                }

                NPC.netUpdate = true;
            }
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.ai[1] > 0f)
            {
                int elapsed = AttackDuration - (int)NPC.ai[1];
                int attackFrame = System.Math.Min(AttackFrameCount - 1,
                    elapsed * AttackFrameCount / AttackDuration);
                NPC.frame.Y = (AttackFirstFrame + attackFrame) * frameHeight;
                return;
            }

            if (System.Math.Abs(NPC.velocity.X) < 0.15f)
            {
                NPC.frame.Y = 0;
                NPC.frameCounter = 0d;
                return;
            }

            NPC.frameCounter++;
            if (NPC.frameCounter >= 5d)
            {
                NPC.frameCounter = 0d;
                int frame = NPC.frame.Y / frameHeight + 1;
                if (frame < WalkFirstFrame || frame > WalkLastFrame)
                    frame = WalkFirstFrame;
                NPC.frame.Y = frame * frameHeight;
            }
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.AddRange(new IBestiaryInfoElement[]
            {
                BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Surface,
                new FlavorTextBestiaryInfoElement("Mods.everflow.Bestiary.AncientTrooper_Melee")
            });
        }
    }
}
