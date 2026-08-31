using everflow.Content.NPCs.Bosses;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Systems
{
    /// <summary>
    /// 睚眦二阶段的环境光压暗。只改变世界环境光，不覆盖界面，
    /// 也不修改时间，因此离开战斗后不会在世界中留下永久状态。
    /// </summary>
    public sealed class YaZiDarknessSystem : ModSystem
    {
        private const int FadeInTime = 20;
        private const int FadeOutTime = 180;
        private const float FadeInStep = 1f / FadeInTime;
        private const float FadeOutStep = 1f / FadeOutTime;
        private float darkness;

        public override void OnWorldLoad()
        {
            darkness = 0f;
        }

        public override void OnWorldUnload()
        {
            darkness = 0f;
        }

        public override void PostUpdateEverything()
        {
            bool darknessRequested = false;
            int headType = ModContent.NPCType<YaZi>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.type != headType)
                    continue;

                if (npc.ModNPC is YaZi yaZi && yaZi.PhaseTwoDarknessActive)
                {
                    darknessRequested = true;
                    break;
                }
            }

            darkness = MathHelper.Clamp(
                darkness + (darknessRequested ? FadeInStep : -FadeOutStep),
                0f,
                1f);
        }

        public override void ModifySunLightColor(
            ref Color tileColor,
            ref Color backgroundColor)
        {
            // 将天空、物块和背景墙的环境光一并压到接近纯黑。
            tileColor = Color.Lerp(tileColor, tileColor * 0.01f, darkness);
            backgroundColor = Color.Lerp(
                backgroundColor,
                backgroundColor * 0.01f,
                darkness);
        }

        public override void ModifyLightingBrightness(ref float scale)
        {
            // 额外压低墙体缓存光和动态光的总体亮度，避免明亮背景墙破坏黑暗感。
            scale *= MathHelper.Lerp(1f, 0.35f, darkness);
        }
    }
}
