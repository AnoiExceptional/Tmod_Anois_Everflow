using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace everflow.Common.Systems
{
    public class EverflowRecipeGroups : ModSystem
    {
        public const string SilverBarGroup = "everflow:SilverBar";
        public const string EvilBarGroup = "everflow:EvilBar";

        public override void AddRecipeGroups()
        {
            // 银锭 / 钨锭
            RecipeGroup silverBarGroup = new RecipeGroup(
                () => "银锭或钨锭",
                ItemID.SilverBar,
                ItemID.TungstenBar
            );

            RecipeGroup.RegisterGroup(
                SilverBarGroup,
                silverBarGroup
            );


            // 恶魔锭 / 猩红锭
            RecipeGroup evilBarGroup = new RecipeGroup(
                () => "恶魔锭或猩红锭",
                ItemID.DemoniteBar,
                ItemID.CrimtaneBar
            );

            RecipeGroup.RegisterGroup(
                EvilBarGroup,
                evilBarGroup
            );
        }
    }
}