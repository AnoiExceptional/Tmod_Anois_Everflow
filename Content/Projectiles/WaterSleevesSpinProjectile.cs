using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using everflow.Content.Items;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class WaterSleevesSpinProjectile : ModProjectile
    {
        private const float RotationPerTick = MathHelper.Pi / 30f;

        public override string Texture =>
            "everflow/Content/Projectiles/WaterSleevesProjectile_2";

        public override void SetDefaults()
        {
            Projectile.width = 192;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
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
            overPlayers.Add(index);
        }

        private float BladeAngle
        {
            get
            {
                float rotationDirection = Projectile.ai[1] >= 0f ? 1f : -1f;
                return Projectile.ai[0] + Projectile.localAI[0] * rotationDirection;
            }
        }

        private static float GetDoubledGlobalAttackSpeed(Player player)
        {
            // GetTotalAttackSpeed会合并Generic与Melee的继承修正，才能覆盖
            // 饰品、盔甲和其他常见的近战/全局攻击速度来源。
            float totalAttackSpeed = player.GetTotalAttackSpeed(DamageClass.Melee);
            // 只把超过基础1倍的最终攻速修正放大两倍；负攻速也等比生效。
            return MathHelper.Max(0.1f, 1f + (totalAttackSpeed - 1f) * 2f);
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed ||
                !player.controlUseTile ||
                player.HeldItem.type != ModContent.ItemType<WaterSleeves>())
            {
                Projectile.Kill();
                return;
            }

            // 右键按住期间只保留这一个射弹，不再每半圈销毁并重建。
            Projectile.timeLeft = 2;
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter);
            // 原贴图的底边中点仍是轴心，整体绘制方向逆时针校正90度。
            Projectile.rotation = BladeAngle;

            // velocity为零的是主袖；副袖只负责相反180度方向的绘制和伤害，
            // 防止两个射弹互相覆盖玩家朝向及手臂姿势。
            if (Projectile.velocity == Vector2.Zero)
            {
                int initialDirection = Projectile.ai[1] >= 0f ? 1 : -1;
                int halfTurn = (int)(Projectile.localAI[0] / MathHelper.Pi);
                int facingDirection = (halfTurn & 1) == 0
                    ? initialDirection
                    : -initialDirection;
                player.ChangeDir(facingDirection);
                player.heldProj = Projectile.whoAmI;
                player.itemTime = player.itemAnimation;
                player.SetCompositeArmFront(
                    true,
                    Player.CompositeArmStretchAmount.Full,
                    BladeAngle - MathHelper.PiOver2 - player.fullRotation);
            }

            float attackSpeed = GetDoubledGlobalAttackSpeed(player);
            Projectile.localNPCHitCooldown = System.Math.Max(
                1, (int)System.MathF.Round(30f / attackSpeed));
            Projectile.localAI[0] += RotationPerTick * attackSpeed;
        }

        private Vector2 RotateLocal(float x, float y)
        {
            if (Projectile.ai[1] < 0f)
                x = -x;
            return Projectile.Center +
                new Vector2(x, y).RotatedBy(Projectile.rotation);
        }

        private static bool HitsLine(
            Rectangle targetHitbox, Vector2 start, Vector2 end, float width)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(), start, end,
                width, ref collisionPoint);
        }

        public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
        {
            // 以贴图底边中点为手部轴心，用三段线贴合弧形飞袖本体。
            Vector2 pivot = Projectile.Center;
            Vector2 leftInner = RotateLocal(-20f, -38f);
            Vector2 leftTip = RotateLocal(-88f, -72f);
            Vector2 rightTip = RotateLocal(88f, -34f);
            return HitsLine(targetHitbox, pivot, leftInner, 34f) ||
                HitsLine(targetHitbox, leftInner, leftTip, 34f) ||
                HitsLine(targetHitbox, pivot, rightTip, 34f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = new Vector2(texture.Width * 0.5f, texture.Height);
            SpriteEffects effects = Projectile.ai[1] < 0f
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                origin,
                1f,
                effects,
                0f);
            return false;
        }
    }
}
