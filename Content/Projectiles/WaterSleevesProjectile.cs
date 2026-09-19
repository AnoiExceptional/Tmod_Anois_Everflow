using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class WaterSleevesProjectile : ModProjectile
    {
        private const int SegmentCount = 5;
        private const int FrameWidth = 30;
        private const int FrameHeight = 46;
        private const int SourceGap = 2;
        private const int SourceTopPadding = 2;
        private const float DrawScale = 1f;
        private const float MaximumSegmentPitch = FrameHeight * DrawScale;
        private const float WeaponForwardOffset = 0f;
        private const float TipCollisionExtension = 15f;
        private const float CollisionWidth = 24f;

        public override string Texture =>
            "everflow/Content/Projectiles/WaterSleevesProjectile_1";

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            // 飞袖从手中向外展开，必须盖在玩家身体和手臂贴图之上。
            overPlayers.Add(index);
        }

        private float Extension
        {
            get
            {
                float age = Projectile.localAI[0];
                int attackDuration = System.Math.Max(2, (int)Projectile.localAI[1]);
                int extendDuration = System.Math.Max(
                    1, (int)System.MathF.Round(attackDuration * 0.6f));
                int holdDuration = System.Math.Max(
                    1, (int)System.MathF.Round(attackDuration / 15f));
                if (extendDuration + holdDuration >= attackDuration)
                    holdDuration = 0;

                if (age < extendDuration)
                {
                    float progress = age /
                        System.Math.Max(1f, extendDuration - 1f);
                    return 1f - (1f - progress) * (1f - progress);
                }
                if (age < extendDuration + holdDuration)
                    return 1f;
                float retractProgress =
                    (age - extendDuration - holdDuration) /
                    System.Math.Max(
                        1f,
                        attackDuration - extendDuration - holdDuration - 1f);
                return 1f - MathHelper.Clamp(retractProgress, 0f, 1f);
            }
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (Projectile.localAI[1] <= 0f)
                Projectile.localAI[1] = System.Math.Max(2, player.itemAnimationMax);
            int attackDuration = (int)Projectile.localAI[1];
            int age = (int)Projectile.localAI[0];
            if (!player.active || player.dead || player.noItems || player.CCed ||
                age >= attackDuration)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Vector2 direction = Projectile.velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            Projectile.velocity = direction;
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter);
            Projectile.rotation = direction.ToRotation() - MathHelper.PiOver2;
            player.ChangeDir(direction.X >= 0f ? 1 : -1);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation;
            player.SetCompositeArmFront(
                true,
                Player.CompositeArmStretchAmount.Full,
                direction.ToRotation() - MathHelper.PiOver2 - player.fullRotation);
            Projectile.localAI[0]++;
        }

        private void GetSleeveLine(out Vector2 start, out Vector2 end)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float pitch = MaximumSegmentPitch * Extension;
            start = Projectile.Center + direction * WeaponForwardOffset;
            end = start + direction * (pitch * (SegmentCount - 1) +
                FrameHeight * DrawScale + TipCollisionExtension);
        }

        public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
        {
            GetSleeveLine(out Vector2 start, out Vector2 end);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(), start, end,
                CollisionWidth, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.75f));
            Projectile.netUpdate = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float pitch = MaximumSegmentPitch * Extension;
            Vector2 origin = new Vector2(FrameWidth * 0.5f, 0f);
            Vector2 weaponBase = Projectile.Center + direction * WeaponForwardOffset;
            for (int i = 0; i < SegmentCount; i++)
            {
                Rectangle source = new Rectangle(
                    0, SourceTopPadding + i * (FrameHeight + SourceGap),
                    FrameWidth, FrameHeight);
                Vector2 position = weaponBase + direction * (pitch * i);
                Main.EntitySpriteDraw(
                    texture, position - Main.screenPosition, source, lightColor,
                    Projectile.rotation, origin, DrawScale, SpriteEffects.None, 0f);
            }
            return false;
        }
    }
}
