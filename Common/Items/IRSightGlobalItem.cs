using everflow.Content.Buffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using System;
using System.Reflection;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Common.Items
{
    /// <summary>
    /// 世界掉落物有独立的GlowMask/全亮绘制路径，必须在整个物品绘制期间
    /// 套用冷物体着色器，避免绕过红外热源分级。
    /// </summary>
    public sealed class IRSightGlobalItem : GlobalItem
    {
        private static Asset<Effect> coldItemEffect;
        private static Asset<Effect> mediumItemEffect;
        private static Hook itemUpdateHook;
        private static Hook vectorLightHook;
        private static Hook tileLightHook;
        private static Hook vectorTorchLightHook;
        private static Hook tileTorchLightHook;
        [ThreadStatic] private static bool suppressWorldItemLight;

        private bool effectApplied;

        private delegate void ItemUpdateOrig(int itemIndex);
        private delegate void ItemUpdateDetour(ItemUpdateOrig orig, int itemIndex);
        private delegate void VectorLightOrig(Vector2 position, float r, float g, float b);
        private delegate void VectorLightDetour(VectorLightOrig orig, Vector2 position, float r, float g, float b);
        private delegate void TileLightOrig(int x, int y, float r, float g, float b);
        private delegate void TileLightDetour(TileLightOrig orig, int x, int y, float r, float g, float b);
        private delegate void VectorTorchLightOrig(Vector2 position, int torchId);
        private delegate void VectorTorchLightDetour(VectorTorchLightOrig orig, Vector2 position, int torchId);
        private delegate void TileTorchLightOrig(int x, int y, int torchId, float lightAmount);
        private delegate void TileTorchLightDetour(TileTorchLightOrig orig, int x, int y, int torchId, float lightAmount);

        public override bool InstancePerEntity => true;

        public override void Load()
        {
            if (!Main.dedServ)
            {
                coldItemEffect = ModContent.Request<Effect>("everflow/Effects/IRSightItem", AssetRequestMode.ImmediateLoad);
                mediumItemEffect = ModContent.Request<Effect>("everflow/Effects/IRSightMediumItem", AssetRequestMode.ImmediateLoad);

                MethodInfo updateItem = typeof(Item).GetMethod(nameof(Item.UpdateItem),
                    BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(int) }, null);
                MethodInfo addVectorLight = typeof(Lighting).GetMethod(nameof(Lighting.AddLight),
                    BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(Vector2), typeof(float), typeof(float), typeof(float) }, null);
                MethodInfo addTileLight = typeof(Lighting).GetMethod(nameof(Lighting.AddLight),
                    BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(int), typeof(int), typeof(float), typeof(float), typeof(float) }, null);
                MethodInfo addVectorTorchLight = typeof(Lighting).GetMethod(nameof(Lighting.AddLight),
                    BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(Vector2), typeof(int) }, null);
                MethodInfo addTileTorchLight = typeof(Lighting).GetMethod(nameof(Lighting.AddLight),
                    BindingFlags.Public | BindingFlags.Static, null,
                    new[] { typeof(int), typeof(int), typeof(int), typeof(float) }, null);

                if (updateItem != null)
                    itemUpdateHook = new Hook(updateItem, (ItemUpdateDetour)UpdateWorldItem);
                if (addVectorLight != null)
                    vectorLightHook = new Hook(addVectorLight, (VectorLightDetour)AddVectorLight);
                if (addTileLight != null)
                    tileLightHook = new Hook(addTileLight, (TileLightDetour)AddTileLight);
                if (addVectorTorchLight != null)
                    vectorTorchLightHook = new Hook(addVectorTorchLight,
                        (VectorTorchLightDetour)AddVectorTorchLight);
                if (addTileTorchLight != null)
                    tileTorchLightHook = new Hook(addTileTorchLight,
                        (TileTorchLightDetour)AddTileTorchLight);
            }
        }

        public override void Unload()
        {
            tileTorchLightHook?.Dispose();
            vectorTorchLightHook?.Dispose();
            tileLightHook?.Dispose();
            vectorLightHook?.Dispose();
            itemUpdateHook?.Dispose();
            tileLightHook = null;
            vectorLightHook = null;
            itemUpdateHook = null;
            tileTorchLightHook = null;
            vectorTorchLightHook = null;
            coldItemEffect = null;
            mediumItemEffect = null;
        }

        public override bool PreDrawInWorld(
            Item item,
            SpriteBatch spriteBatch,
            Color lightColor,
            Color alphaColor,
            ref float rotation,
            ref float scale,
            int whoAmI)
        {
            Asset<Effect> selectedEffect = IsMediumHeatItem(item)
                ? mediumItemEffect
                : coldItemEffect;
            effectApplied = InfraredIsActive() && selectedEffect?.Value != null;
            if (!effectApplied)
                return true;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer,
                selectedEffect.Value, Main.GameViewMatrix.TransformationMatrix);
            return true;
        }

        private static void UpdateWorldItem(ItemUpdateOrig orig, int itemIndex)
        {
            bool previous = suppressWorldItemLight;
            suppressWorldItemLight = InfraredIsActive();
            try
            {
                orig(itemIndex);
            }
            finally
            {
                suppressWorldItemLight = previous;
            }
        }

        private static void AddVectorLight(VectorLightOrig orig, Vector2 position, float r, float g, float b)
        {
            if (!suppressWorldItemLight)
                orig(position, r, g, b);
        }

        private static void AddTileLight(TileLightOrig orig, int x, int y, float r, float g, float b)
        {
            if (!suppressWorldItemLight)
                orig(x, y, r, g, b);
        }

        private static void AddVectorTorchLight(
            VectorTorchLightOrig orig,
            Vector2 position,
            int torchId)
        {
            // 原版放置、掉落和手持火把最终都会经过火把ID专用重载。
            // 红外视野中火把是只自发高亮、不向周围投光的中热源。
            if (!InfraredIsActive())
                orig(position, torchId);
        }

        private static void AddTileTorchLight(
            TileTorchLightOrig orig,
            int x,
            int y,
            int torchId,
            float lightAmount)
        {
            if (!InfraredIsActive())
                orig(x, y, torchId, lightAmount);
        }

        private static bool IsMediumHeatItem(Item item)
        {
            if (ItemID.Sets.Torches[item.type] || ItemID.Sets.Glowsticks[item.type])
                return true;

            return item.createTile >= 0 && item.createTile < TileID.Sets.Campfire.Length &&
                TileID.Sets.Campfire[item.createTile];
        }

        public override void PostDrawInWorld(
            Item item,
            SpriteBatch spriteBatch,
            Color lightColor,
            Color alphaColor,
            float rotation,
            float scale,
            int whoAmI)
        {
            if (!effectApplied)
                return;

            effectApplied = false;
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
