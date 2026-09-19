using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Content.Items.Materials
{
    public sealed class WyrmBladeSharp : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 16;
            Item.height = 16;
            Item.maxStack = Item.CommonMaxStack;
            Item.material = true;
            Item.rare = ItemRarityID.Orange;
        }
    }
}
