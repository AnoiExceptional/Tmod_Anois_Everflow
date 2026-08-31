using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Combat;

namespace everflow.Content.Projectiles
{
    public class DragonsEdgeWyvernProjectile : ModProjectile
    {
        private const int SegmentCount = 13;
        private const float SegmentSpacing = 28f;
        private static readonly Color WyvernColor = new(192, 144, 169);

        public override string Texture =>
            "everflow/Content/Projectiles/DragonsEdgeProjectile_1";

        public override void SetStaticDefaults()
        {
            // 原版飞龙各分节是带透明留白的单帧整图，不是纵向三帧动画表。
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.MinionShot[Type] = true;
            // 整条龙由头部这一枚弹幕统一绘制。扩大屏幕裁剪范围，
            // 避免头部暂时离屏时仍在屏幕内的身体被整条跳过绘制。
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 44;
            // 伤害由13枚随体节移动的独立碰撞弹幕负责。
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.maxPenetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.usesIDStaticNPCImmunity = false;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int segmentHitboxType =
                ModContent.ProjectileType<DragonsEdgeWyvernSegmentHitbox>();

            for (int i = 0; i < SegmentCount; i++)
            {
                Projectile.NewProjectile(
                    source,
                    GetSegmentCenter(i),
                    Projectile.velocity,
                    segmentHitboxType,
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    Projectile.identity,
                    i);
            }
        }

        public override void AI()
        {
            DragonTargeting.CorrectCourse(
                Projectile,
                maximumTurnDegrees: 3f,
                maximumLeadFrames: 18f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            // 沿整条龙躯发出与改色贴图相同的粉紫色光。
            Vector3 lightColor = WyvernColor.ToVector3() * 0.45f;
            for (int i = 0; i < SegmentCount; i += 2)
                Lighting.AddLight(GetSegmentCenter(i), lightColor);

            // 大量同色调附魔粒子沿各身体分节散落。
            for (int i = 0; i < 5; i++)
            {
                int segment = Main.rand.Next(SegmentCount);
                Vector2 position = GetSegmentCenter(segment) +
                    Main.rand.NextVector2Circular(18f, 18f);
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.Enchanted_Pink,
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.07f) +
                        Main.rand.NextVector2Circular(1.2f, 1.2f),
                    80,
                    WyvernColor,
                    Main.rand.NextFloat(0.75f, 1.15f));
                dust.noGravity = true;
                dust.fadeIn = 1.1f;
            }

            // 由弹幕拥有者判断整条龙是否已经完整进出屏幕，再同步消失。
            if (Projectile.owner == Main.myPlayer)
            {
                Rectangle viewport = new(
                    (int)Main.screenPosition.X,
                    (int)Main.screenPosition.Y,
                    Main.screenWidth,
                    Main.screenHeight);
                viewport.Inflate(80, 80);

                bool anySegmentOnScreen = false;
                for (int i = 0; i < SegmentCount; i++)
                {
                    if (viewport.Contains(GetSegmentCenter(i).ToPoint()))
                    {
                        anySegmentOnScreen = true;
                        break;
                    }
                }

                if (anySegmentOnScreen)
                {
                    Projectile.localAI[0] = 1f;
                }
                else if (Projectile.localAI[0] == 1f)
                {
                    Projectile.Kill();
                }
            }
        }

        internal Vector2 GetSegmentCenter(int index)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            return Projectile.Center - direction * (index * SegmentSpacing);
        }

        private static string GetSegmentTexturePath(int index)
        {
            // 头-身-身-足-身-身-身-足-身-身-尾1-尾2-尾3
            return index switch
            {
                0 => "everflow/Content/Projectiles/DragonsEdgeProjectile_1",
                3 or 7 => "everflow/Content/Projectiles/DragonsEdgeProjectile_1_Legs",
                10 => "everflow/Content/Projectiles/DragonsEdgeProjectile_1_Body2",
                11 => "everflow/Content/Projectiles/DragonsEdgeProjectile_1_Body3",
                12 => "everflow/Content/Projectiles/DragonsEdgeProjectile_1_Tail",
                _ => "everflow/Content/Projectiles/DragonsEdgeProjectile_1_Body"
            };
        }

        public override bool? Colliding(Rectangle projectileHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            for (int i = 0; i < SegmentCount - 1; i++)
            {
                if (Collision.CheckAABBvLineCollision(
                    targetHitbox.TopLeft(),
                    targetHitbox.Size(),
                    GetSegmentCenter(i),
                    GetSegmentCenter(i + 1),
                    30f,
                    ref collisionPoint))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            SpriteEffects effects = forward.X < 0f
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            // 从尾到头绘制，使前方分节自然覆盖后方分节。
            for (int i = SegmentCount - 1; i >= 0; i--)
            {
                Texture2D texture = ModContent.Request<Texture2D>(
                    GetSegmentTexturePath(i)).Value;
                Rectangle frame = texture.Frame();

                Vector2 center = GetSegmentCenter(i);
                Vector2 nextCenter = i < SegmentCount - 1
                    ? GetSegmentCenter(i + 1)
                    : center - forward;
                Vector2 segmentForward = (center - nextCenter)
                    .SafeNormalize(forward);
                float rotation = segmentForward.ToRotation() + MathHelper.PiOver2;
                Color color = Lighting.GetColor(center.ToTileCoordinates()) * 0.5f;

                Main.EntitySpriteDraw(
                    texture,
                    center - Main.screenPosition,
                    frame,
                    color,
                    rotation,
                    frame.Size() * 0.5f,
                    1f,
                    effects,
                    0f);
            }

            return false;
        }
    }
}
