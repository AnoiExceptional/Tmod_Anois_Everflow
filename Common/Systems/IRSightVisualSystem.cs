using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Common.Systems
{
    [Autoload(Side = ModSide.Client)]
    public sealed class IRSightVisualSystem : ModSystem
    {
        private const string FilterKey = "everflow:IRSight";

        private static bool IsActive =>
            !Main.gameMenu && Main.LocalPlayer != null &&
            Main.LocalPlayer.HasBuff(ModContent.BuffType<IRSightBuff>());

        public override void Load()
        {
            Asset<Effect> effect = ModContent.Request<Effect>(
                "everflow/Effects/IRSight",
                AssetRequestMode.ImmediateLoad);

            Filters.Scene[FilterKey] = new Filter(
                new ScreenShaderData(effect, "IRSightPass").UseOpacity(1f),
                EffectPriority.VeryHigh);
            Filters.Scene[FilterKey].Load();
        }

        public override void Unload()
        {
            if (Filters.Scene[FilterKey]?.IsActive() == true)
                Filters.Scene.Deactivate(FilterKey);
        }

        public override void PostUpdateEverything()
        {
            Filter filter = Filters.Scene[FilterKey];
            if (IsActive)
            {
                if (!filter.IsActive())
                {
                    Filters.Scene.Activate(FilterKey, Main.LocalPlayer.Center);
                    // 红外开关是驾驶功能，不使用原版场景滤镜的渐入。
                    filter.Opacity = 1f;
                }
            }
            else if (filter.IsActive())
            {
                Filters.Scene.Deactivate(FilterKey);
                // Filter.Deactivate只把Active设为false，Opacity仍会缓慢衰减；
                // 直接清零可让黑白画面与Buff在同一帧关闭。
                filter.Opacity = 0f;
            }

            if (IsActive)
            {
                AddVisibleLavaHeat();
            }
        }

        private static void AddVisibleLavaHeat()
        {
            int left = Utils.Clamp((int)(Main.screenPosition.X / 16f) - 2, 0, Main.maxTilesX - 1);
            int right = Utils.Clamp((int)((Main.screenPosition.X + Main.screenWidth) / 16f) + 2, 0, Main.maxTilesX - 1);
            int top = Utils.Clamp((int)(Main.screenPosition.Y / 16f) - 2, 0, Main.maxTilesY - 1);
            int bottom = Utils.Clamp((int)((Main.screenPosition.Y + Main.screenHeight) / 16f) + 2, 0, Main.maxTilesY - 1);

            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    Tile tile = Framing.GetTileSafely(x, y);
                    if (tile.LiquidAmount == 0 || tile.LiquidType != LiquidID.Lava)
                        continue;

                    float heat = 1.65f * tile.LiquidAmount / byte.MaxValue;
                    Lighting.AddLight(new Vector2(x * 16f + 8f, y * 16f + 8f), heat, heat, heat);
                }
            }
        }

        public override void ModifySunLightColor(
            ref Color tileColor,
            ref Color backgroundColor)
        {
            if (!IsActive)
                return;

            // 普通物块维持最低可辨识度。墙和非地狱远景仍是深暗灰，
            // 但不再彻底吞没；地狱远景作为高温环境单独提高亮度。
            tileColor = ToGray(tileColor, 0.34f, 18);
            backgroundColor = Main.LocalPlayer.ZoneUnderworldHeight
                ? ToGray(backgroundColor, 0.18f, 20)
                : ToGray(backgroundColor, 0.085f, 7);
        }

        private static Color ToGray(Color source, float strength, byte minimum)
        {
            float luminance =
                source.R * 0.299f + source.G * 0.587f + source.B * 0.114f;
            byte gray = (byte)MathHelper.Clamp(luminance * strength, minimum, 255f);
            return new Color(gray, gray, gray, source.A);
        }
    }
}
