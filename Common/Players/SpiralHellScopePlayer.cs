using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using everflow.Content.Items;

namespace everflow.Common.Players
{
    /// <summary>
    /// Gives Equilibrio an always-on, sniper-scope-style camera extension.
    /// Vanilla scope movement normally depends on holding right click, but
    /// Spiral Hell reserves right click for changing forms, so this version
    /// follows the cursor automatically while Equilibrio is held.
    /// </summary>
    public sealed class SpiralHellScopePlayer : ModPlayer
    {
        private Vector2 currentScopeOffset;

        public override void ModifyScreenPosition()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            bool scopeActive = Player.active
                && !Player.dead
                && Player.HeldItem.ModItem is SpiralHellEquilibrio
                && !Main.gameMenu
                && !Main.mapFullscreen;

            Vector2 targetOffset = Vector2.Zero;
            if (scopeActive)
            {
                Vector2 screenCenter = new Vector2(Main.screenWidth, Main.screenHeight) * 0.5f;
                Vector2 clampedMouse = new Vector2(
                    MathHelper.Clamp(Main.mouseX, 0f, Main.screenWidth),
                    MathHelper.Clamp(Main.mouseY, 0f, Main.screenHeight));

                // Match the scope concept: shift the camera halfway from the
                // screen center toward the cursor. Clamping the cursor keeps
                // the maximum extension resolution-relative and predictable.
                targetOffset = (clampedMouse - screenCenter) * 0.5f;
            }

            currentScopeOffset = Vector2.Lerp(currentScopeOffset, targetOffset,
                scopeActive ? 0.12f : 0.2f);

            if (currentScopeOffset.LengthSquared() < 0.01f)
                currentScopeOffset = Vector2.Zero;

            Main.screenPosition += currentScopeOffset;
        }
    }
}
