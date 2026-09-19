using everflow.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class WyrmBladeCharge : ModProjectile
    {
        private const int ChargeTime = 120;
        private const int AnimationTime = 60;
        private const int TransformationDelay = 15;
        private const int EmpoweredHoldStart = AnimationTime + TransformationDelay;
        private const int TotalManaCost = 50;
        private static readonly Color ChargeColor = new Color(255, 213, 65);

        public override string Texture =>
            "everflow/Content/Items/WyrmBladeGreatsword_1";

        private ref float Charge => ref Projectile.ai[0];
        private ref float ManaSpent => ref Projectile.ai[1];
        private ref float TransformationSucceeded => ref Projectile.ai[2];

        private bool HasSucceeded => TransformationSucceeded >= 1f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 68;
            Projectile.height = 68;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.hide = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead || player.noItems || player.CCed)
            {
                Projectile.Kill();
                return;
            }

            if (!HasSucceeded)
            {
                if (player.HeldItem.type != ModContent.ItemType<WyrmBladeGreatsword>())
                {
                    Projectile.Kill();
                    return;
                }

                // 成功前必须持续按住右键。
                if (Projectile.owner == Main.myPlayer)
                {
                    if (!player.controlUseTile)
                    {
                        player.channel = false;
                        Projectile.Kill();
                        return;
                    }

                    player.channel = true;
                }
                else if (!player.channel)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else if (Projectile.owner == Main.myPlayer && player.controlUseItem)
            {
                // 成功后的45帧只是展示；左键攻击可以立即打断展示，Buff与
                // 强化物品均保留，由原版真近战攻击直接接管。
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter);
            if (!HasSucceeded)
            {
                player.heldProj = Projectile.whoAmI;
                player.itemTime = 2;
                player.itemAnimation = 2;
                player.itemRotation = -MathHelper.PiOver2 - player.fullRotation;
            }

            Charge++;

            if (Main.netMode != NetmodeID.Server)
            {
                // _1的四帧在60帧内产生三次帧切换；每次切换播放一次
                // 升高一个八度的召唤杖音效，总计三次。
                if (Charge == 20f || Charge == 40f || Charge == 60f)
                {
                    float summonPitch = Charge == 20f
                        ? -0.5f
                        : Charge == 40f
                            ? -0.25f
                            : 0f;
                    SoundEngine.PlaySound(
                        SoundID.Item44 with
                        {
                            Pitch = summonPitch,
                            PitchVariance = 0f
                        },
                        Projectile.Center);
                }

            }

            int desiredManaSpent = System.Math.Min(
                TotalManaCost,
                (int)(Charge * TotalManaCost / EmpoweredHoldStart));
            int manaToPay = desiredManaSpent - (int)ManaSpent;
            if (manaToPay > 0)
            {
                bool ownsManaState = Main.netMode == NetmodeID.Server ||
                    Projectile.owner == Main.myPlayer;
                if (ownsManaState && player.statMana < manaToPay)
                {
                    Projectile.Kill();
                    return;
                }

                if (ownsManaState)
                {
                    // 直接扣除本次分摊魔力，避免CheckMana产生逐次消耗音效；
                    // statMana的实际下降仍可被水德等全局魔力监听正确记录。
                    player.statMana -= manaToPay;
                    player.manaRegenDelay = player.maxRegenDelay;
                }

                ManaSpent += manaToPay;
            }

            if (!HasSucceeded && Charge >= EmpoweredHoldStart)
            {
                // 利刃台风音效响起的这一帧即为不可撤销的成功点：50魔力
                // 已全部支付，立即切换形态并开始15秒Buff倒计时。
                if (Main.netMode == NetmodeID.Server || Projectile.owner == Main.myPlayer)
                    WyrmBladeGreatsword.EnterEmpoweredForm(player);

                if (Main.netMode != NetmodeID.Server)
                {
                    SoundEngine.PlaySound(
                        SoundID.Item84,
                        Projectile.Center);
                }

                TransformationSucceeded = 1f;
                Projectile.netUpdate = true;

                // 解除右键使用锁定。之后松开右键不会取消，左键可以攻击。
                player.itemAnimation = 0;
                player.itemTime = 0;
                player.channel = false;
                player.heldProj = -1;
            }

            Projectile.frame = (int)MathHelper.Clamp(
                Charge * 3f / AnimationTime,
                0f,
                3f);

            Vector2 effectCenter = player.RotatedRelativePoint(player.MountedCenter) -
                Vector2.UnitY * 38f;
            Lighting.AddLight(effectCenter, ChargeColor.ToVector3() * 0.55f);
            if (Main.rand.NextBool(3))
            {
                Vector2 dustPosition = player.RotatedRelativePoint(player.MountedCenter) +
                    new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-74f, -8f));
                Dust dust = Dust.NewDustPerfect(
                    dustPosition,
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(0.55f, 0.55f),
                    70,
                    ChargeColor,
                    Main.rand.NextFloat(0.55f, 0.85f));
                dust.noGravity = true;
            }

            if (Charge >= ChargeTime)
                Projectile.Kill();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            bool drawEmpoweredBlade = Charge >= EmpoweredHoldStart;
            Texture2D texture = drawEmpoweredBlade
                ? ModContent.Request<Texture2D>(
                    "everflow/Content/Items/WyrmBladeGreatsword_2").Value
                : TextureAssets.Projectile[Type].Value;
            Rectangle frame = drawEmpoweredBlade
                ? texture.Bounds
                : texture.Frame(1, 4, 0, Projectile.frame);
            Vector2 drawPosition = player.RotatedRelativePoint(player.MountedCenter) - Main.screenPosition;

            // 两张图都以左下角作为剑柄锚点。切换到90x90的_2时原点
            // 仍为(0, 高度)，所以剑柄位置固定不跳动，剑尖继续朝上。
            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                frame,
                lightColor,
                -MathHelper.PiOver4,
                new Vector2(0f, frame.Height),
                1f,
                SpriteEffects.None);

            if (drawEmpoweredBlade)
            {
                Texture2D glowTexture = ModContent.Request<Texture2D>(
                    "everflow/Content/Projectiles/WyrmBladeGreatsword_Glow").Value;
                Main.EntitySpriteDraw(
                    glowTexture,
                    drawPosition,
                    glowTexture.Bounds,
                    Color.White,
                    -MathHelper.PiOver4,
                    new Vector2(0f, glowTexture.Height),
                    1f,
                    SpriteEffects.None);
            }

            return false;
        }
    }
}
