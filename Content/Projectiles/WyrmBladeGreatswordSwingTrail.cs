using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Projectiles
{
    /// <summary>
    /// 连阙剑强化形态挥舞时的纯视觉剑光。
    /// 与天顶剑一样，记录连续挥舞角度并用一条宽轨迹带填满剑刃扫过的扇面，
    /// 而不是重复绘制若干把历史剑身。
    /// </summary>
    public sealed class WyrmBladeGreatswordSwingTrail : ModProjectile
    {
        private const int LeftTrailLength = 20;
        private const int SpinChargeTime = 30;
        private const int SpinSlashTime = 15;
        private const int TrailLength = 20;
        private const float TrailBackwardAngle = MathHelper.Pi * 2f / 9f;
        private const float TrailHalfWidth = 45f;
        private const float TrailOuterRadius = 145f;
        private const float TrailCenterDistance = TrailOuterRadius - TrailHalfWidth;
        private static readonly Color RuneGold = new Color(255, 213, 65);
        private readonly VertexStrip trail = new VertexStrip();
        private readonly Vector2[] leftTrailPositions = new Vector2[LeftTrailLength];
        private readonly float[] leftTrailRotations = new float[LeftTrailLength];
        private readonly Vector2[] spinTrailPositions = new Vector2[SpinSlashTime];
        private readonly float[] spinTrailRotations = new float[SpinSlashTime];

        public override string Texture =>
            "everflow/Content/Projectiles/WyrmBladeGreatsword_Glow";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = TrailLength;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.DontAttachHideToAlpha[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.hide = true;
            // 只覆盖当前这一次20帧挥舞；不能由玩家后续自动挥舞的
            // itemAnimation续命，否则旧剑光会永久叠加。
            Projectile.timeLeft = 20;
            Projectile.netImportant = false;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void DrawBehind(
            int index,
            List<int> behindNPCsAndTiles,
            List<int> behindNPCs,
            List<int> behindProjectiles,
            List<int> overPlayers,
            List<int> overWiresUI)
        {
            // 该缓存先于普通射弹与玩家绘制，因此剑身及其发光遮罩会自然
            // 覆盖在剑光之上，不需要改变FinalFractal本身的绘制方式。
            behindProjectiles.Add(index);
        }

        public override void OnSpawn(IEntitySource source)
        {
            UpdateFromOwner();
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Projectile.oldPos[i] = Projectile.position;
                Projectile.oldRot[i] = Projectile.rotation;
            }
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            int age = (int)Projectile.localAI[0]++;
            bool spinAttack = Projectile.ai[0] == 1f;
            bool inSpinSlash = age >= SpinChargeTime &&
                age < SpinChargeTime + SpinSlashTime;
            if (player.itemAnimation > 0 && (!spinAttack || inSpinSlash))
            {
                UpdateFromOwner();

                // 蓄力结束的第一帧从剑尖朝上的位置重新建立整条历史缓存，
                // 避免连接到生成射弹时尚未摆正的旧持握角度。
                if (spinAttack && age == SpinChargeTime)
                {
                    for (int i = 0; i < Projectile.oldPos.Length; i++)
                    {
                        Projectile.oldPos[i] = Projectile.position;
                        Projectile.oldRot[i] = Projectile.rotation;
                    }
                }
            }
        }

        private void UpdateFromOwner()
        {
            Player player = Main.player[Projectile.owner];

            // itemRotation在朝左时使用镜像坐标；还原为世界空间中的实际剑刃方向。
            Vector2 bladeDirection = player.itemRotation.ToRotationVector2();
            bladeDirection.X *= player.direction;
            if (player.direction == -1)
                bladeDirection.Y *= -1f;

            // 剑光的能量面需要贴着咒文剑刃的剑脊展开；在此前30°基础上
            // 再向挥舞后方微调10°，总后偏角为40°。左右朝向反向镜像。
            Vector2 trailDirection = bladeDirection.RotatedBy(
                -TrailBackwardAngle * player.direction);

            float attackSpeedScale = Projectile.ai[0] == 1f
                ? System.MathF.Max(0.1f, Projectile.ai[1])
                : 1f;
            float effectScale = Projectile.ai[0] == 1f
                ? 2f * attackSpeedScale
                : 1f;

            // FinalFractal的oldRot并非剑刃径向角本身，而是径向角+90°。
            // VertexStrip据此计算横截面，少掉这90°就会变成错误的锯齿折扇。
            Projectile.rotation = trailDirection.ToRotation() + MathHelper.PiOver2;

            // 中线放在100px处，加上45px恒定半宽后，剑光外缘为145px。
            Projectile.Center = player.RotatedRelativePoint(player.MountedCenter) +
                trailDirection * TrailCenterDistance * effectScale;
        }

        private Color TrailColor(float progress)
        {
            // 原版FinalFractalProfile.StripColors：主体保持完整颜色，
            // 只在轨迹末端的最后2%收至透明，并将Alpha减半交给专用Shader混合。
            Color color = RuneGold *
                (1f - Utils.GetLerpValue(0f, 0.98f, progress, false));
            color.A /= 2;
            return color;
        }

        private float TrailWidth(float progress)
        {
            // FinalFractalProfile使用恒定半宽；90px贴图对应45px半宽。
            return Projectile.ai[0] == 1f
                ? TrailHalfWidth * 2f *
                    System.MathF.Max(0.1f, Projectile.ai[1])
                : TrailHalfWidth;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            bool spinAttack = Projectile.ai[0] == 1f;
            int age = (int)Projectile.localAI[0];
            if (spinAttack &&
                (age <= SpinChargeTime || age > SpinChargeTime + SpinSlashTime))
                return false;

            // 保留原版FinalFractal专用Shader与两张噪声纹理，但这里只绘制
            // 一次持剑挥舞，因此幻影分段数必须为1。原版传入4是为天顶剑
            // 弹幕的四组illusion服务，直接照搬会把20帧挥舞割成多块光片。
            MiscShaderData shader = GameShaders.Misc["FinalFractal"];
            shader.UseShaderSpecificData(new Vector4(1f, 0f, 0f, 1f));
            shader.UseImage0("Images/Extra_201");
            shader.UseImage1("Images/Extra_193");
            shader.Apply();

            Vector2[] positions = Projectile.oldPos;
            float[] rotations = Projectile.oldRot;
            int sampleCount = Projectile.oldPos.Length;

            if (!spinAttack)
            {
                // 左键仍只使用最近20帧；右键使用完整45帧来覆盖一整圈。
                for (int i = 0; i < LeftTrailLength; i++)
                {
                    leftTrailPositions[i] = Projectile.oldPos[i];
                    leftTrailRotations[i] = Projectile.oldRot[i];
                }

                positions = leftTrailPositions;
                rotations = leftTrailRotations;
                sampleCount = LeftTrailLength;
            }
            else
            {
                // 旋斩只绘制中间15帧的一整圈，蓄力与冷却均不留下剑光。
                for (int i = 0; i < SpinSlashTime; i++)
                {
                    spinTrailPositions[i] = Projectile.oldPos[i];
                    spinTrailRotations[i] = Projectile.oldRot[i];
                }

                positions = spinTrailPositions;
                rotations = spinTrailRotations;
                sampleCount = SpinSlashTime;
            }

            trail.PrepareStrip(
                positions,
                rotations,
                TrailColor,
                TrailWidth,
                -Main.screenPosition + Projectile.Size * 0.5f,
                sampleCount,
                includeBacksides: true);
            trail.DrawTrail();

            // FinalFractalHelper.Draw在结束后恢复默认像素Shader，防止污染后续绘制。
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            return false;
        }
    }
}
