using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Content.Items;

namespace everflow.Content.Projectiles
{
    public sealed class BurgerKingProjectile : ModProjectile
    {
        private readonly int[] fillings = new int[BurgerKing.MaximumFillingLayers];
        private int fillingCount;
        private int explosionSize;

        public override string Texture => "everflow/Content/Items/BurgerKing";

        public override void SetDefaults()
        {
            Projectile.width = BurgerKing.FrameWidth;
            Projectile.height = BurgerKing.EmptyBurgerHeight;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        internal void Configure(int[] selectedFillings, int penetrationBonus, int configuredExplosionSize)
        {
            fillingCount = System.Math.Min(selectedFillings?.Length ?? 0, fillings.Length);
            for (int i = 0; i < fillingCount; i++)
                fillings[i] = selectedFillings[i];

            Projectile.penetrate = 1 + System.Math.Max(0, penetrationBonus);
            explosionSize = System.Math.Max(0, configuredExplosionSize);
        }

        public override void AI()
        {
            Projectile.velocity.Y = MathHelper.Min(Projectile.velocity.Y + 0.12f, 16f);
            Projectile.rotation += Projectile.velocity.X * 0.045f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BurgerKing.DrawBurger(
                Main.spriteBatch,
                Projectile.Center - Main.screenPosition,
                lightColor,
                Projectile.rotation,
                new Vector2(BurgerKing.FrameWidth, BurgerKing.FrameHeight) * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                GetFillingFrames());
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < fillingCount; i++)
            {
                if (!BurgerKingFillingCatalog.TryGet(fillings[i], out BurgerKingFillingDefinition filling))
                    continue;

                int buffType = filling.Debuff switch
                {
                    BurgerKingFillingDebuff.Poisoned => BuffID.Poisoned,
                    BurgerKingFillingDebuff.CursedInferno => BuffID.CursedInferno,
                    BurgerKingFillingDebuff.Hellfire => BuffID.OnFire3,
                    BurgerKingFillingDebuff.Ichor => BuffID.Ichor,
                    _ => 0
                };

                if (buffType > 0)
                    target.AddBuff(buffType, 300);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (explosionSize <= 0)
                return;

            Vector2 center = Projectile.Center;
            Projectile.Resize(explosionSize, explosionSize);
            Projectile.Center = center;
            Projectile.penetrate = -1;
            Projectile.Damage();

            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    DustID.Smoke,
                    Main.rand.NextVector2Circular(4f, 4f),
                    80,
                    new Color(197, 136, 85),
                    Main.rand.NextFloat(1f, 1.6f));
                dust.noGravity = true;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)fillingCount);
            for (int i = 0; i < fillingCount; i++)
                writer.Write((byte)fillings[i]);
            writer.Write(Projectile.penetrate);
            writer.Write(explosionSize);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            fillingCount = System.Math.Min(reader.ReadByte(), fillings.Length);
            for (int i = 0; i < fillingCount; i++)
                fillings[i] = reader.ReadByte();
            Projectile.penetrate = reader.ReadInt32();
            explosionSize = reader.ReadInt32();
        }

        private int[] GetFillingFrames()
        {
            int[] result = new int[fillingCount];
            for (int i = 0; i < fillingCount; i++)
                result[i] = fillings[i];
            return result;
        }
    }

    public sealed class BurgerKingEatingVisual : ModProjectile
    {
        public override string Texture => "everflow/Content/Items/BurgerKing";

        public override void SetDefaults()
        {
            Projectile.width = BurgerKing.FrameWidth;
            Projectile.height = BurgerKing.EmptyBurgerHeight;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            Projectile.localAI[0]++;
            if (!player.active || player.dead ||
                player.itemAnimation <= 0 ||
                player.HeldItem.type != ModContent.ItemType<BurgerKing>() ||
                player.HeldItem.useStyle != ItemUseStyleID.EatFood ||
                Projectile.localAI[0] > 30f)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            float progress = 1f - player.itemAnimation / (float)System.Math.Max(1, player.itemAnimationMax);
            float towardMouth = (float)System.Math.Pow(System.Math.Abs(System.Math.Sin(progress * MathHelper.Pi * 3f)), 0.7);
            Vector2 mouth = player.MountedCenter + new Vector2(player.direction * 6f, -11f);
            Vector2 away = mouth + new Vector2(player.direction * 22f, 8f);
            Projectile.Center = Vector2.Lerp(away, mouth, towardMouth);
            Projectile.rotation = player.direction * MathHelper.Lerp(-0.22f, 0.08f, towardMouth);
            Projectile.spriteDirection = player.direction;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects effects = Projectile.spriteDirection < 0
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            int[] fillings = playerHeldBurgerFillings();
            BurgerKing.DrawBurger(
                Main.spriteBatch,
                Projectile.Center - Main.screenPosition,
                lightColor,
                Projectile.rotation,
                new Vector2(BurgerKing.FrameWidth, BurgerKing.FrameHeight) * 0.5f,
                Projectile.scale,
                effects,
                fillings);
            return false;
        }

        private int[] playerHeldBurgerFillings()
        {
            Player player = Main.player[Projectile.owner];
            return player.HeldItem.ModItem is BurgerKing burger
                ? burger.GetSelectedFillingFrames()
                : System.Array.Empty<int>();
        }
    }
}
