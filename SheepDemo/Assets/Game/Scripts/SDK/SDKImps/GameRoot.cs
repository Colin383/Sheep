#nullable enable
using System.Collections.Generic;
using Guru.SDK.Framework.Utils.Package;

namespace Game
{
    public class GameRoot : GuruRoot
    {
        public GameRoot(List<GuruPackage> children, int priority = 0) : base(children, priority)
        {
            // 默认没有组件包, 只有主包
            if (children == null)
            {
                children = new List<GuruPackage>();
                
            }
            
            //TODO: 项目组可以在此添加自己针对组件包的调用和初始化
        }
    }
}