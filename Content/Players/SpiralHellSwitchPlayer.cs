using Terraria;
using Terraria.ModLoader;

namespace everflow.Content.Players
{
    /// <summary>
    /// Preserves the player's pre-switch facing direction because Shoot-style
    /// item handling otherwise turns the player toward the mouse each frame.
    /// </summary>
    public sealed class SpiralHellSwitchPlayer : ModPlayer
    {
        public int LockedFacingDirection { get; private set; }
        private int lastFreeFacingDirection = 1;

        public override void Initialize()
        {
            LockedFacingDirection = 0;
            lastFreeFacingDirection = Player.direction == 0 ? 1 : Player.direction;
        }

        public void LockFacingDirection()
        {
            if (LockedFacingDirection != 0)
                return;

            if (Player.controlLeft != Player.controlRight)
                LockedFacingDirection = Player.controlLeft ? -1 : 1;
            else
                LockedFacingDirection = lastFreeFacingDirection;
        }

        public override void PostUpdate()
        {
            if (Player.itemAnimation <= 0)
            {
                LockedFacingDirection = 0;
                lastFreeFacingDirection = Player.direction == 0
                    ? lastFreeFacingDirection
                    : Player.direction;
            }
        }
    }
}
