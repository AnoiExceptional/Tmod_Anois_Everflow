using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using everflow.Common.Combat;

namespace everflow.Content.Projectiles
{
    public class DragonsEdgePhantasmProjectile : ModProjectile
    {
        private const int SegmentCount = 19;
        private const float SegmentSpacing = 84f;
        private const float DrawScale = 3f;
        private static readonly Color PhantasmColor = new(150, 104, 136);

        public override string Texture =>
            "everflow/Content/Projectiles/DragonsEdgeProjectile_3";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2200;
        }

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
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
                ModContent.ProjectileType<DragonsEdgePhantasmSegmentHitbox>();
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
                maximumTurnDegrees: 2f,
                maximumLeadFrames: 12f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Vector3 lightColor = PhantasmColor.ToVector3() * 0.45f;
            for (int i = 0; i < SegmentCount; i += 2)
                Lighting.AddLight(GetSegmentCenter(i), lightColor);

            for (int i = 0; i < 8; i++)
            {
                Vector2 position = GetSegmentCenter(Main.rand.Next(SegmentCount)) +
                    Main.rand.NextVector2Circular(36f, 36f);
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.Enchanted_Pink,
                    -Projectile.velocity * Main.rand.NextFloat(0.015f, 0.05f) +
                        Main.rand.NextVector2Circular(1.5f, 1.5f),
                    80,
                    PhantasmColor,
                    Main.rand.NextFloat(0.9f, 1.4f));
                dust.noGravity = true;
                dust.fadeIn = 1.15f;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                Rectangle viewport = new(
                    (int)Main.screenPosition.X,
                    (int)Main.screenPosition.Y,
                    Main.screenWidth,
                    Main.screenHeight);
                viewport.Inflate(200, 200);

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
            return Projectile.Center - direction * (index * SegmentSpacing);
        }

        private static string GetSegmentTexturePath(int index)
        {
            // 头-身-身-身-足-身-身-身-足-身-身-身-足-身-身-身-尾1-尾2-尾3
            return index switch
            {
                0 => "everflow/Content/Projectiles/DragonsEdgeProjectile_3",
                4 or 8 or 12 => "everflow/Content/Projectiles/DragonsEdgeProjectile_3_Body",
                16 => "everflow/Content/Projectiles/DragonsEdgeProjectile_3_Body3",
                17 => "everflow/Content/Projectiles/DragonsEdgeProjectile_3_Body4",
                18 => "everflow/Content/Projectiles/DragonsEdgeProjectile_3_Tail",
                _ => "everflow/Content/Projectiles/DragonsEdgeProjectile_3_Body2"
            };
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
