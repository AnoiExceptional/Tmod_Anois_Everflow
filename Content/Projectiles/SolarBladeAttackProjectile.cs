using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using everflow.Content.Buffs;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    public sealed class SolarBladeAttackProjectile : ModProjectile
    {
        private const float SourceBladeAngle = -MathHelper.PiOver4;
        private const float DrawScale = 2f;
        private const float CollisionWidth = 28f;
        private const int TrailLength = 96;
        private const float TrailHalfWidth = 90f;
        private const float MaximumThrustExtension = 82f;
        private const float BladeReachExtension = 16f;
        private const float MaximumSpinSampleAngle = MathHelper.Pi / 36f;
        private readonly VertexStrip trail = new();
        private readonly Vector2[] trailPositions = new Vector2[TrailLength];
        private readonly float[] trailRotations = new float[TrailLength];
        private bool trailInitialized;
        private float lastTrailBladeAngle;
        private float previousCollisionAngle;
        private float currentCollisionAngle;

        public override string Texture =>
            "everflow/Content/Items/SolarBlade_Blue";

        private int AttackType => (int)Projectile.ai[0];
        private int ColorIndex => (int)Projectile.ai[1];
        private int ItemType => (int)Projectile.ai[2];
        private bool IsThrust => AttackType == 3;
        private float AimAngle => Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
        private Color EffectColor => GetEffectColor(ColorIndex);

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.scale = DrawScale;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = ModContent.GetInstance<
                global::everflow.Common.DamageClasses.MagicalMeleeDamageClass>();
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (Projectile.localAI[1] <= 0f)
                Projectile.localAI[1] = System.Math.Max(2, player.itemAnimationMax);
            int duration = (int)Projectile.localAI[1];
            int age = (int)Projectile.localAI[0]++;
            if (!player.active || player.dead || player.noItems || player.CCed ||
                age >= duration)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Vector2 aimDirection = Projectile.velocity.SafeNormalize(
                Vector2.UnitX * player.direction);
            int facing = aimDirection.X >= 0f ? 1 : -1;
            player.ChangeDir(facing);
            Vector2 pivot = player.RotatedRelativePoint(player.MountedCenter);

            // 所有四段攻击均保持恒定的200%视觉与判定尺寸。
            Projectile.scale = DrawScale;

            float bladeAngle;
            if (IsThrust)
            {
                float extension = GetThrustExtension(age, duration);
                Projectile.Center = pivot + aimDirection *
                    (10f + MaximumThrustExtension * extension);
                bladeAngle = AimAngle;
            }
            else
            {
                Projectile.Center = pivot;
                bladeAngle = GetSwingAngle(age, duration);
                previousCollisionAngle = trailInitialized
                    ? lastTrailBladeAngle
                    : bladeAngle;
                currentCollisionAngle = bladeAngle;
                UpdateTrail(pivot, bladeAngle);
            }

            Projectile.rotation = bladeAngle - SourceBladeAngle;
            player.heldProj = Projectile.whoAmI;
            player.itemTime = player.itemAnimation;
            player.SetCompositeArmFront(
                true,
                Player.CompositeArmStretchAmount.Full,
                bladeAngle - MathHelper.PiOver2 - player.fullRotation);

            Vector2 lightPosition = Projectile.Center +
                bladeAngle.ToRotationVector2() * (GetBladeReach() * 0.5f);
            Lighting.AddLight(lightPosition, EffectColor.ToVector3() * 0.9f);
        }

        private float GetSwingAngle(int age, int duration)
        {
            int slashFrames = System.Math.Max(
                1,
                (int)System.MathF.Round(duration * 0.82f));
            float progress = MathHelper.Clamp(
                age / (float)System.Math.Max(1, slashFrames - 1),
                0f,
                1f);

            // 前两段半圆斩击采用先加速后减速的角速度：
            // v(t)=vMin*(1+sin(pi*t))，因此中点最大角速度恰为
            // 起止最小角速度的两倍；对速度积分并归一化后仍在原有
            // slashFrames内精确走完180度。第三段整周旋转保留原曲线。
            float halfSwingProgress = GetTwoToOneAngularProgress(progress);
            float spinProgress = progress * progress;

            return AttackType switch
            {
                0 => AimAngle + MathHelper.Lerp(
                    -MathHelper.PiOver2,
                    MathHelper.PiOver2,
                    halfSwingProgress),
                1 => AimAngle + MathHelper.Lerp(
                    MathHelper.PiOver2,
                    -MathHelper.PiOver2,
                    halfSwingProgress),
                // 从上一击的-90度终点继续正向旋转整整一周。
                _ => AimAngle - MathHelper.PiOver2 + MathHelper.TwoPi * spinProgress
            };
        }

        private static float GetTwoToOneAngularProgress(float timeProgress)
        {
            float numerator = timeProgress +
                (1f - System.MathF.Cos(MathHelper.Pi * timeProgress)) /
                MathHelper.Pi;
            float denominator = 1f + 2f / MathHelper.Pi;
            return numerator / denominator;
        }

        private static float GetThrustExtension(int age, int duration)
        {
            int extendFrames = System.Math.Max(
                1,
                (int)System.MathF.Round(duration * 0.62f));
            if (age < extendFrames)
            {
                float progress = age /
                    (float)System.Math.Max(1, extendFrames - 1);
                return 1f - (1f - progress) * (1f - progress);
            }

            float retract = (age - extendFrames) /
                (float)System.Math.Max(1, duration - extendFrames - 1);
            return 1f - MathHelper.Clamp(retract, 0f, 1f);
        }

        private void UpdateTrail(Vector2 pivot, float bladeAngle)
        {
            float trailCenterDistance =
                GetBladeReach() + BladeReachExtension - TrailHalfWidth;
            Vector2 center = pivot +
                bladeAngle.ToRotationVector2() * trailCenterDistance;
            float rotation = bladeAngle + MathHelper.PiOver2;
            if (!trailInitialized)
            {
                trailInitialized = true;
                lastTrailBladeAngle = bladeAngle;
                for (int i = 0; i < TrailLength; i++)
                {
                    trailPositions[i] = center;
                    trailRotations[i] = rotation;
                }
                return;
            }

            float angleDelta = bladeAngle - lastTrailBladeAngle;
            int sampleCount = AttackType == 2
                ? System.Math.Clamp(
                    (int)System.MathF.Ceiling(
                        System.MathF.Abs(angleDelta) / MaximumSpinSampleAngle),
                    1,
                    32)
                : 1;

            // 第三段高速旋转时，在相邻逻辑帧的角度之间插入中间点。
            // 每插入一个点都像正常一帧一样推进轨迹数组，因此得到的是
            // 沿圆周分布的密集采样，而不是把直线边简单重复多次。
            for (int sample = 1; sample <= sampleCount; sample++)
            {
                float sampleAngle = MathHelper.Lerp(
                    lastTrailBladeAngle,
                    bladeAngle,
                    sample / (float)sampleCount);
                Vector2 sampleCenter = pivot +
                    sampleAngle.ToRotationVector2() * trailCenterDistance;
                float sampleRotation = sampleAngle + MathHelper.PiOver2;
                for (int i = TrailLength - 1; i > 0; i--)
                {
                    trailPositions[i] = trailPositions[i - 1];
                    trailRotations[i] = trailRotations[i - 1];
                }
                trailPositions[0] = sampleCenter;
                trailRotations[0] = sampleRotation;
            }
            lastTrailBladeAngle = bladeAngle;
        }

        public override bool? Colliding(
            Rectangle projectileHitbox,
            Rectangle targetHitbox)
        {
            if (AttackType != 2 || !trailInitialized)
            {
                GetBladeLine(out Vector2 start, out Vector2 end);
                float collisionPoint = 0f;
                return Collision.CheckAABBvLineCollision(
                    targetHitbox.TopLeft(),
                    targetHitbox.Size(),
                    start,
                    end,
                    CollisionWidth * Projectile.scale / DrawScale,
                    ref collisionPoint);
            }

            float angleDelta = currentCollisionAngle - previousCollisionAngle;
            int sampleCount = System.Math.Clamp(
                (int)System.MathF.Ceiling(
                    System.MathF.Abs(angleDelta) / MaximumSpinSampleAngle),
                1,
                32);
            for (int sample = 0; sample <= sampleCount; sample++)
            {
                float angle = MathHelper.Lerp(
                    previousCollisionAngle,
                    currentCollisionAngle,
                    sample / (float)sampleCount);
                GetBladeLineAtAngle(angle, out Vector2 start, out Vector2 end);
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(
                    targetHitbox.TopLeft(),
                    targetHitbox.Size(),
                    start,
                    end,
                    CollisionWidth * Projectile.scale / DrawScale,
                    ref collisionPoint))
                {
                    return true;
                }
            }
            return false;
        }

        public override void OnHitNPC(
            NPC target,
            NPC.HitInfo hit,
            int damageDone)
        {
            EnergyCutDebuff.Apply(target);
        }

        private void GetBladeLine(out Vector2 start, out Vector2 end)
        {
            float bladeAngle = Projectile.rotation + SourceBladeAngle;
            GetBladeLineAtAngle(bladeAngle, out start, out end);
        }

        private void GetBladeLineAtAngle(
            float bladeAngle,
            out Vector2 start,
            out Vector2 end)
        {
            Texture2D texture = GetWeaponTexture();
            Vector2 origin = GetWeaponOrigin(texture);
            Vector2 localTip = new Vector2(
                texture.Width - 3f,
                3f) - origin;
            start = Projectile.Center;
            Vector2 bladeVector = (localTip * Projectile.scale).RotatedBy(
                bladeAngle - SourceBladeAngle);
            end = Projectile.Center + bladeVector +
                bladeVector.SafeNormalize(Vector2.UnitX) * BladeReachExtension;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = GetWeaponTexture();
            if (!IsThrust)
                DrawSwingTrail();

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                GetWeaponOrigin(texture),
                Projectile.scale,
                SpriteEffects.None,
                0f);

            if (IsThrust)
                DrawThrustFlare(texture);
            return false;
        }

        private void DrawSwingTrail()
        {
            if (!trailInitialized)
                return;

            MiscShaderData shader = GameShaders.Misc["FinalFractal"];
            // 与虫戟矛完全相同的FinalFractal控制参数。颜色由下方的
            // TrailColor顶点色提供，不能把颜色再次塞进控制参数，否则
            // 会被Shader二次相乘并显著变暗。
            shader.UseShaderSpecificData(new Vector4(1f, 0f, 0f, 1f));
            shader.UseImage0("Images/Extra_201");
            shader.UseImage1("Images/Extra_193");
            shader.Apply();
            trail.PrepareStrip(
                trailPositions,
                trailRotations,
                TrailColor,
                _ => TrailHalfWidth,
                -Main.screenPosition,
                TrailLength,
                includeBacksides: true);
            trail.DrawTrail();
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
        }

        private Color TrailColor(float progress)
        {
            Color color = EffectColor * (1.5f *
                (1f - Utils.GetLerpValue(0f, 0.98f, progress, false)));
            color.A /= 2;
            return color;
        }

        private void DrawThrustFlare(Texture2D weaponTexture)
        {
            int duration = System.Math.Max(2, (int)Projectile.localAI[1]);
            int age = (int)Projectile.localAI[0];
            float strength = GetThrustExtension(age, duration);
            if (strength <= 0f)
                return;

            Vector2 direction = AimAngle.ToRotationVector2();
            Vector2 origin = GetWeaponOrigin(weaponTexture);
            Vector2 localTip = new Vector2(
                weaponTexture.Width - 3f,
                3f) - origin;
            Vector2 tip = Projectile.Center +
                (localTip * Projectile.scale).RotatedBy(Projectile.rotation);
            Texture2D flare = TextureAssets.Extra[98].Value;
            Vector2 flareOrigin = flare.Size() * 0.5f;
            float rotation = AimAngle + MathHelper.PiOver2;
            SpriteEffects effects = direction.X < 0f
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            Color color = new Color(
                EffectColor.R,
                EffectColor.G,
                EffectColor.B,
                0) * strength;

            Main.EntitySpriteDraw(
                flare,
                tip - Main.screenPosition,
                null,
                color,
                rotation,
                flareOrigin,
                new Vector2(strength, 1.5f) * DrawScale,
                effects);
            for (float trailProgress = 0.5f;
                trailProgress <= 1f;
                trailProgress += 0.1f)
            {
                Vector2 position = tip - direction *
                    ((1f - trailProgress) * 34f * DrawScale);
                Main.EntitySpriteDraw(
                    flare,
                    position - Main.screenPosition,
                    null,
                    color * (0.7f * trailProgress),
                    rotation,
                    flareOrigin,
                    new Vector2(
                        strength * trailProgress,
                        2f * strength * trailProgress) * DrawScale,
                    effects);
            }
        }

        private Texture2D GetWeaponTexture()
        {
            Main.instance.LoadItem(ItemType);
            return TextureAssets.Item[ItemType].Value;
        }

        private float GetBladeReach()
        {
            Texture2D texture = GetWeaponTexture();
            Vector2 origin = GetWeaponOrigin(texture);
            Vector2 localTip = new Vector2(
                texture.Width - 3f,
                3f) - origin;
            return localTip.Length() * Projectile.scale;
        }

        private static Vector2 GetWeaponOrigin(Texture2D texture) =>
            new Vector2(8f, texture.Height - 8f);

        private static Color GetEffectColor(int colorIndex) => colorIndex switch
        {
            0 => new Color(40, 130, 255),
            1 => new Color(255, 45, 35),
            2 => new Color(45, 255, 95),
            3 => new Color(190, 70, 255),
            4 => new Color(235, 245, 255),
            5 => new Color(255, 230, 45),
            6 => new Color(255, 130, 25),
            _ => Color.White
        };
    }
}
