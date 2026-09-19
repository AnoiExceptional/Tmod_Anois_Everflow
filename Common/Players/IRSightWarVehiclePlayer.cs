using everflow.Content.Buffs;
using everflow.Content.Mounts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace everflow.Common.Players
{
    /// <summary>
    /// 将战争载具及其驾驶员作为一个完整的中热源处理。TransformDrawData
    /// 在全部原版/Mod玩家绘制层生成后执行，因此车体、炮塔和武器不会漏掉。
    /// </summary>
    public sealed class IRSightWarVehiclePlayer : ModPlayer
    {
        private static readonly Color MediumHeat = new(168, 168, 168);

        public override void PostUpdate()
        {
            if (!InfraredIsActive() || !IsWarVehicle(Player))
                return;

            // 战争载具是实际的中等热光源。以玩家（即车体碰撞箱）中心
            // 投射灰白光，使车体、炮塔及未隐藏的驾驶员一起明显变亮。
            Lighting.AddLight(Player.Center, 0.55f, 0.55f, 0.55f);
        }

        public override void TransformDrawData(ref PlayerDrawSet drawInfo)
        {
            if (!InfraredIsActive() || !IsWarVehicle(drawInfo.drawPlayer))
                return;

            for (int i = 0; i < drawInfo.DrawDataCache.Count; i++)
            {
                DrawData data = drawInfo.DrawDataCache[i];
                byte alpha = data.color.A;
                data.color = new Color(MediumHeat.R, MediumHeat.G, MediumHeat.B, alpha);
                drawInfo.DrawDataCache[i] = data;
            }
        }

        private static bool IsWarVehicle(Player player)
        {
            if (!player.mount.Active)
                return false;

            int mountType = player.mount.Type;
            return mountType == ModContent.MountType<AlloyTank02>() ||
                   mountType == ModContent.MountType<BMPT72>();
        }

        private static bool InfraredIsActive() =>
            !Main.dedServ && !Main.gameMenu && Main.LocalPlayer != null &&
            Main.LocalPlayer.HasBuff(ModContent.BuffType<IRSightBuff>());
    }
}
