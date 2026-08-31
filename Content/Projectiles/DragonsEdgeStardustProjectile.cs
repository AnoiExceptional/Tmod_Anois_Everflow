using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Combat;

namespace everflow.Content.Projectiles
{
    public class DragonsEdgeStardustProjectile : ModProjectile
    {
        private const int SegmentCount = 14;
        private const float SegmentSpacing = 36f;
        private const float DrawScale = 2f;
        private static readonly Color StardustColor = new(226, 114, 133);

        public override string Texture =>
            "everflow/Content/Projectiles/DragonsEdgeProjectile_2";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.maxPenetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
            Projectile.netImportant = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int hitboxType =
                ModContent.ProjectileType<DragonsEdgeStardustSegmentHitbox>();
            for (int i = 0; i < SegmentCount; i++)
            {
                Projectile.NewProjectile(
                    source,
                    GetSegmentCenter(i),
                    Projectile.velocity,
                    hitboxType,
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
                maximumTurnDegrees: 2.5f,
                maximumLeadFrames: 16f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Vector3 lightColor = StardustColor.ToVector3() * 0.45f;
            for (int i = 0; i < SegmentCount; i += 2)
                Lighting.AddLight(GetSegmentCenter(i), lightColor);

            for (int i = 0; i < 5; i++)
            {
                Vector2 position = GetSegmentCenter(Main.rand.Next(SegmentCount)) +
                    Main.rand.NextVector2Circular(12f, 12f);
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.Enchanted_Pink,
                    -Projectile.velocity * Main.rand.NextFloat(0.02f, 0.07f) +
                        Main.rand.NextVector2Circular(1.2f, 1.2f),
                    80,
                    StardustColor,
                    Main.rand.NextFloat(0.7f, 1.1f));
                dust.noGravity = true;
                dust.fadeIn = 1.1f;
            }

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
                    Projectile.localAI[0] = 1f;
                else if (Projectile.localAI[0] == 1f)
                    Projectile.Kill();
            }
        }

        internal Vector2 GetSegmentCenter(int index)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);
            float wave = index == 0
                ? 0f
                : (float)System.Math.Sin(
                    Main.GameUpdateCount * 0.12f - index * 0.65f) * (20f / 3f);

            return Projectile.Center - direction * (index * SegmentSpacing) +
                perpendicular * wave;
        }

        private static string GetSegmentTexturePath(int index)
        {
            if (index == 0)
                return "everflow/Content/Projectiles/DragonsEdgeProjectile_2";
            if (index == SegmentCount - 1)
                return "everflow/Content/Projectiles/DragonsEdgeProjectile_2_Tail";

            return index % 2 == 1
                ? "everflow/Content/Projectiles/DragonsEdgeProjectile_2_Body"
                : "everflow/Content/Projectiles/DragonsEdgeProjectile_2_Body2";
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            SpriteEffects effects = forward.X < 0f
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

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
                    DrawScale,
                    effects,
                    0f);
            }

            return false;
        }
    }
}
