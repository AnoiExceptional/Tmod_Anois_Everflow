using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace everflow.Common.Projectiles
{
    /// <summary>
    /// 反射后停用原敌对弹幕AI，防止蒲公英孢子等弹幕每帧把速度重新转回
    /// 原来的漂浮方向。弹幕仍会按反射瞬间的速度正常移动、碰撞和消失。
    /// </summary>
    public sealed class ForceReflectedGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        internal bool IsReflected;

        public override bool PreAI(Projectile projectile) => !IsReflected;

        public override void SendExtraAI(
            Projectile projectile,
            BitWriter bitWriter,
            BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(IsReflected);
        }

        public override void ReceiveExtraAI(
            Projectile projectile,
            BitReader bitReader,
            BinaryReader binaryReader)
        {
            IsReflected = bitReader.ReadBit();
        }
    }
}
