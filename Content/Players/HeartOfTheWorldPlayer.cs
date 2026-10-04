using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Players
{
    public sealed class HeartOfTheWorldPlayer : ModPlayer
    {
        public bool Equipped { get; set; }

        public bool PureHeartEquipped { get; set; }

        public bool TerraBladeUsePurified { get; set; }

        public override void ResetEffects()
        {
            Equipped = false;
            PureHeartEquipped = false;
        }

        public override void PostUpdate()
        {
            if (Player.itemAnimation <= 0 || Player.HeldItem.type != ItemID.TerraBlade)
                TerraBladeUsePurified = false;
        }

        public bool CanPurifyNextTerraBladeUse() =>
            Equipped && (
                (Player.itemAnimation > 0 && TerraBladeUsePurified) ||
                Player.HasItem(ItemID.GreenSolution));
    }
}
