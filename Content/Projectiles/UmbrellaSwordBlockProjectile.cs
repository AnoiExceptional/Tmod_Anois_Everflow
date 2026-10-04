using everflow.Content.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class UmbrellaSwordBlockProjectile : ModProjectile
    {
        private const int BlockDuration = 20;

        public override string Texture => "everflow/Content/Projectiles/UmbrellaSword_Block";

        public override void SetDefaults()
        {
            Projectile.width = 46;
            Projectile.height = 54;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = BlockDuration;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            UmbrellaSwordPlayer blockPlayer = player.GetModPlayer<UmbrellaSwordPlayer>();
            if (!player.active || player.dead || player.HeldItem.type != ModContent.ItemType<global::everflow.Content.Items.UmbrellaSword>() ||
                blockPlayer.ParryCooldown > 0)
            {
                Projectile.Kill();
                return;
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(new Vector2(player.direction, 0f));
            player.ChangeDir(direction.X >= 0f ? 1 : -1);

            int elapsed = BlockDuration - Projectile.timeLeft;
            float extension = MathHelper.Lerp(8f, 34f, Utils.GetLerpValue(0f, 5f, elapsed, true));
            Projectile.Center = player.MountedCenter + direction * extension;
            Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity = direction;
            blockPlayer.BlockDirection = direction;

            player.heldProj = Projectile.whoAmI;
            player.itemTime = 2;
            player.itemAnimation = 2;
            player.itemRotation = direction.ToRotation();
            // 总动画保持20帧，仅中间10帧（第6至15帧）有效。
            // 一旦本次格挡成功，剩余动画不再重复触发格挡。
            if (!blockPlayer.BlockConsumed && elapsed >= 5 && elapsed < 15)
                blockPlayer.BlockTime = System.Math.Max(blockPlayer.BlockTime, 2);
            else
                blockPlayer.BlockTime = 0;
        }

        public override void OnKill(int timeLeft)
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active)
                return;

            // 每次格挡动作结束都进入20帧冷却，无论是否成功挡到攻击。
            UmbrellaSwordPlayer blockPlayer = player.GetModPlayer<UmbrellaSwordPlayer>();
            blockPlayer.BlockTime = 0;
            blockPlayer.ParryCooldown = System.Math.Max(blockPlayer.ParryCooldown, 20);

            if (player.HeldItem.type != ModContent.ItemType<global::everflow.Content.Items.UmbrellaSword>())
                return;

            // 右键结束时立刻恢复左键阔剑状态，避免 noUseGraphic 残留到
            // 下一次左键挥舞，造成第一下武器贴图不可见。
            // 必须先清除右键残留的物品动画，否则恢复 noMelee 后，收伞末尾
            // 的一两帧会被原版当作一次近战刺击并产生伤害。
            player.itemAnimation = 0;
            player.itemTime = 0;
            if (player.heldProj == Projectile.whoAmI)
                player.heldProj = -1;

            Item item = player.HeldItem;
            item.noUseGraphic = false;
            item.noMelee = false;
            item.useStyle = ItemUseStyleID.Swing;
            item.useTime = 22;
            item.useAnimation = 22;
            item.shoot = ProjectileID.None;
            item.shootSpeed = 0f;
            item.UseSound = SoundID.Item1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                texture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None);
            return false;
        }
    }
}
