using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class WyrmBladeSpearSwingProjectile : ModProjectile
    {
        // 原贴图从右下枪柄指向左上枪头，即贴图空间中的-135度方向。
        private const float SourceBladeAngle = -MathHelper.Pi * 0.75f;
        private const float CollisionWidth = 36f;
        private const float PivotInset = 36f;
        private const int TrailLength = 25;
        private const float TrailHalfWidth = 90f;
        private const float TrailOuterRadius = 305.47013f;
        private const float TrailCenterDistance = TrailOuterRadius - TrailHalfWidth;
        private static readonly Color TrailTint = new Color(210, 75, 107);
        private readonly VertexStrip trail = new VertexStrip();
        private readonly Vector2[] trailPositions = new Vector2[TrailLength];
        private readonly float[] trailRotations = new float[TrailLength];
        private bool trailInitialized;

        public override string Texture =>
            "everflow/Content/Projectiles/WyrmBladeSpearProjectile_2";

        private float AimAngle => Projectile.ai[0];
        private int SwingDirection => Projectile.ai[1] >= 0f ? 1 : -1;
        private int FacingDirection => AimAngle.ToRotationVector2().X >= 0f ? 1 : -1;

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.scale = 2f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.usesIDStaticNPCImmunity = false;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (Projectile.localAI[1] <= 0f)
                Projectile.localAI[1] = System.Math.Max(2, player.itemAnimationMax);
            int attackDuration = (int)Projectile.localAI[1];
            int age = (int)Projectile.localAI[0]++;
            if (!player.active || player.dead || player.noItems || player.CCed ||
                age >= attackDuration)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter);
            player.ChangeDir(FacingDirection);
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation;

            // 25帧动作拆为20帧加速斩击和5帧终点停顿。前20帧使用
            // 二次曲线令角速度持续增加，并在第20帧精确走完180度；
            // 后5帧progress保持1，使枪身带着力量感停在斩击终点。
            int slashFrames = System.Math.Max(
                1,
                (int)System.MathF.Round(attackDuration * 0.8f));
            float progress = MathHelper.Clamp(
                age / (float)System.Math.Max(1, slashFrames - 1),
                0f,
                1f);
            progress *= progress;

            // 第2击从鼠标方向-90度扫到+90度；第4击的SwingDirection
            // 为-1，因此从+90度反向扫回-90度。瞄准角在生成时锁定。
            float relativeSwingAngle = MathHelper.Lerp(
                -MathHelper.PiOver2,
                MathHelper.PiOver2,
                progress) * SwingDirection;
            float bladeAngle = AimAngle + relativeSwingAngle;
            Projectile.rotation = bladeAngle - SourceBladeAngle;
            UpdateTrail(Projectile.Center, bladeAngle);

            player.SetCompositeArmFront(
                true,
                Player.CompositeArmStretchAmount.Full,
                bladeAngle - MathHelper.PiOver2 - player.fullRotation);
        }

        private void UpdateTrail(Vector2 pivot, float bladeAngle)
        {
            Vector2 bladeDirection = bladeAngle.ToRotationVector2();
            Vector2 trailCenter = pivot + bladeDirection * TrailCenterDistance;
            float trailRotation = bladeAngle + MathHelper.PiOver2;

            if (!trailInitialized)
            {
                trailInitialized = true;
                for (int i = 0; i < TrailLength; i++)
                {
                    trailPositions[i] = trailCenter;
                    trailRotations[i] = trailRotation;
                }
                return;
            }

            for (int i = TrailLength - 1; i > 0; i--)
            {
                trailPositions[i] = trailPositions[i - 1];
                trailRotations[i] = trailRotations[i - 1];
            }
            trailPositions[0] = trailCenter;
            trailRotations[0] = trailRotation;
        }

        public override bool? Colliding(
            Rectangle projectileHitbox,
            Rectangle targetHitbox)
        {
            GetBladeLine(out Vector2 tail, out Vector2 tip);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                tail,
                tip,
                CollisionWidth,
                ref collisionPoint);
        }

        private void GetBladeLine(out Vector2 tail, out Vector2 tip)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 pivotOrigin = GetPivotOrigin(texture);

            // 碰撞线的两端直接使用贴图空间中的枪尾与枪头，围绕与绘制
            // 完全相同的新轴心旋转，确保轴心内移后判定不会留在旧位置。
            Vector2 localTail = new Vector2(
                texture.Width - pivotOrigin.X,
                texture.Height - pivotOrigin.Y) * Projectile.scale;
            Vector2 localTip = -pivotOrigin * Projectile.scale;
            tail = Projectile.Center + localTail.RotatedBy(Projectile.rotation);
            tip = Projectile.Center + localTip.RotatedBy(Projectile.rotation);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 pivotOrigin = GetPivotOrigin(texture);

            DrawZenithTrail();

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.Lerp(lightColor, TrailTint, 0.18f),
                Projectile.rotation,
                pivotOrigin,
                Projectile.scale,
                SpriteEffects.None,
                0f);
            return false;
        }

        private void DrawZenithTrail()
        {
            if (!trailInitialized)
                return;

            // 与连阙剑强化形态相同的FinalFractal绘制管线：一次攻击
            // 只提交一条连续轨迹带，避免多片剑光叠加得越来越亮。
            MiscShaderData shader = GameShaders.Misc["FinalFractal"];
            shader.UseShaderSpecificData(new Vector4(1f, 0f, 0f, 1f));
            shader.UseImage0("Images/Extra_201");
            shader.UseImage1("Images/Extra_193");
            shader.Apply();

            trail.PrepareStrip(
                trailPositions,
                trailRotations,
                TrailColor,
                TrailWidth,
                -Main.screenPosition,
                TrailLength,
                includeBacksides: true);
            trail.DrawTrail();

            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }

        private static Color TrailColor(float progress)
        {
            Color color = TrailTint *
                (1f - Utils.GetLerpValue(0f, 0.98f, progress, false));
            color.A /= 2;
            return color;
        }

        private static float TrailWidth(float progress) => TrailHalfWidth;

        private static Vector2 GetPivotOrigin(Texture2D texture) => new Vector2(
            texture.Width - PivotInset,
            texture.Height - PivotInset);
    }
}
