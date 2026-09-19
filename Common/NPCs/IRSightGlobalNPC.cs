using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace everflow.Common.NPCs
{
    /// <summary>
    /// 只修改NPC原本绘制管线收到的照明颜色，不重新绘制贴图。
    /// 因而动态换帧、换形态和自定义PreDraw都保持原样。
    /// </summary>
    public sealed class IRSightGlobalNPC : GlobalNPC
    {
        private static Asset<Effect> heatEffect;
        private bool heatEffectApplied;

        public override bool InstancePerEntity => true;

        public override void Load()
        {
            if (!Main.dedServ)
                heatEffect = ModContent.Request<Effect>("everflow/Effects/IRSightNPC", AssetRequestMode.ImmediateLoad);
        }

        public override void Unload() => heatEffect = null;

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (!InfraredIsActive())
                return;

            // 只把NPC自身交给白热着色器；这里绝不调用Lighting.AddLight，
            // 所以敌怪、动物和城镇NPC都不会照亮身边的物块。
            drawColor = Color.White;
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            heatEffectApplied = InfraredIsActive() && heatEffect?.Value != null;
            if (!heatEffectApplied)
                return true;

            spriteBatch.End();
            // 专用着色器忽略贴图RGB，只保留其透明度并输出统一的亮白色。
            // 因此深色史莱姆和浅色城镇NPC会具有相同的热目标亮度。
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                heatEffect.Value, Main.GameViewMatrix.TransformationMatrix);
            return true;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!heatEffectApplied)
                return;

            heatEffectApplied = false;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                null, Main.GameViewMatrix.TransformationMatrix);
        }

        private static bool InfraredIsActive() =>
            !Main.dedServ && !Main.gameMenu && Main.LocalPlayer != null &&
            Main.LocalPlayer.HasBuff(ModContent.BuffType<IRSightBuff>());
    }
}
