using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Projectiles;

namespace everflow.Content.NPCs
{
    public sealed class AncientTrooper_Ranged : ModNPC
    {
        private const int FrameCount = 26;
        private const int WalkFirstFrame = 1;
        private const int WalkLastFrame = 14;
        private const int AttackFirstFrame = 21;
        private const int AttackFrameCount = 5;
        private const int AttackDuration = 25;
        private const int MinimumAttackCooldown = 50;
        private const int MaximumAttackCooldown = 60;
        private const float PreferredAttackDistance = 20f * 16f;
        private const float MaximumAttackDistance = 23f * 16f;
        private const float MoveSpeed = 1.5f;
        private const float MoveAcceleration = 0.06f;
        private const float ArrowSpeed = 8f;
        private const float ArrowGravity = 0.1f;
        private const int BaseAttackDamage = 10;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            NPC.width = 18;
            NPC.height = 40;
            NPC.damage = 10;
            NPC.defense = 6;
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
            Vector2 toTarget = target.Center - NPC.Center;
            int direction = toTarget.X >= 0f ? 1 : -1;
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
                    NPC.velocity.X *= 0.82f;
                return;
            }

            NPC.ai[0]++;
            if (NPC.ai[3] < MinimumAttackCooldown && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[3] = Main.rand.Next(MinimumAttackCooldown, MaximumAttackCooldown + 1);
                NPC.netUpdate = true;
            }

            float horizontalDistance = System.Math.Abs(toTarget.X);
            if (horizontalDistance > PreferredAttackDistance)
            {
                NPC.velocity.X = MathHelper.Clamp(
                    NPC.velocity.X + direction * MoveAcceleration,
                    -MoveSpeed,
                    MoveSpeed);
            }
            else
            {
                NPC.velocity.X *= 0.75f;
            }

            int obstacleTileX = (int)((NPC.Center.X + direction * (NPC.width * 0.5f + 8f)) / 16f);
            int feetTileY = (int)(NPC.Bottom.Y / 16f);
            bool obstacleAhead = WorldGen.SolidTile(obstacleTileX, feetTileY - 1) ||
                WorldGen.SolidTile(obstacleTileX, feetTileY - 2);
            bool targetIsReachablyAbove = target.Center.Y <= NPC.Center.Y - 32f &&
                horizontalDistance <= 10f * 16f;

            if (standingOnGround && (NPC.collideX || obstacleAhead || targetIsReachablyAbove))
            {
                NPC.velocity.Y = -6f;
                NPC.netUpdate = true;
            }

            bool canShoot = toTarget.Length() <= MaximumAttackDistance &&
                Collision.CanHitLine(NPC.position, NPC.width, NPC.height,
                    target.position, target.width, target.height);
            NPC.ai[2] = canShoot && horizontalDistance <= PreferredAttackDistance ? 1f : 0f;
            if (canShoot && NPC.ai[0] >= NPC.ai[3] &&
                Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.ai[0] = 0f;
                NPC.ai[1] = AttackDuration;
                NPC.ai[3] = Main.rand.Next(MinimumAttackCooldown, MaximumAttackCooldown + 1);
                if (standingOnGround)
                    NPC.velocity.X *= 0.35f;

                Vector2 arrowDirection = GetCompensatedAimDirection(target)
                    .RotatedByRandom(MathHelper.ToRadians(5f));
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    NPC.Center + new Vector2(0f, -6f),
                    arrowDirection * ArrowSpeed,
                    ModContent.ProjectileType<AncientTrooperArrow>(),
                    NPC.GetAttackDamage_ForProjectiles(
                        BaseAttackDamage * 0.5f,
                        BaseAttackDamage * 0.5f),
                    3f,
                    Main.myPlayer);

                NPC.netUpdate = true;
            }
        }

        private Vector2 GetCompensatedAimDirection(Player target)
        {
            float horizontalDistance = System.Math.Abs(target.Center.X - NPC.Center.X);
            float estimatedFlightTime = horizontalDistance / ArrowSpeed;
            float verticalCompensation = 0.5f * ArrowGravity *
                estimatedFlightTime * estimatedFlightTime;
            verticalCompensation = MathHelper.Clamp(verticalCompensation, 8f, 96f);

            Vector2 compensatedTarget = target.Center - Vector2.UnitY * verticalCompensation;
            return (compensatedTarget - (NPC.Center + new Vector2(0f, -6f)))
                .SafeNormalize(Vector2.UnitX * NPC.direction);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (NPC.ai[2] != 1f || !NPC.HasValidTarget)
                return;

            Vector2 aimDirection = GetCompensatedAimDirection(Main.player[NPC.target]);
            Texture2D bowTexture = ModContent.Request<Texture2D>(
                "everflow/Content/Items/AlloyBow").Value;
            Vector2 drawPosition = NPC.Center + new Vector2(0f, -6f) +
                aimDirection * 13f - screenPos;
            float rotation = aimDirection.ToRotation();
            SpriteEffects effects = aimDirection.X < 0f
                ? SpriteEffects.FlipVertically
                : SpriteEffects.None;

            spriteBatch.Draw(
                bowTexture,
                drawPosition,
                null,
                drawColor,
                rotation,
                bowTexture.Size() * 0.5f,
                1f,
                effects,
                0f);
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
            if (NPC.frameCounter >= 6d)
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
                new FlavorTextBestiaryInfoElement("Mods.everflow.Bestiary.AncientTrooper_Ranged")
            });
        }
    }
}
