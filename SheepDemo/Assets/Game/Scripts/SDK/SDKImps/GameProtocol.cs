using Guru.SDK.Framework.Core.Spec.Protocols;
using Guru.SDK.Framework.Core.Spec.Protocols.Account;
using Guru.SDK.Framework.Core.Spec.Protocols.Ads;
using Guru.SDK.Framework.Core.Spec.Protocols.Analytics;
using Guru.SDK.Framework.Core.Spec.Protocols.Game;
using Guru.SDK.Framework.Core.Spec.Protocols.Migration;
using Guru.SDK.Framework.Core.Spec.Protocols.Crash;

namespace Game
{
    
    /// <summary>
    /// 游戏协议实现类
    /// </summary>
    public class GameProtocol: IGuruSdkProtocol
    {
        public IGameAttributeDelegate GameAttributeDelegate => GameAttribute.Instance;
        public IAccountAuthDelegate AccountAuthDelegate { get; }
        public IAdsSpecDelegate AdsSpecDelegate => GameAdsSpec.Instance;
        public IAnalyticsDelegate AnalyticsDelegate => GameAnalytics.Instance;
        public MigrationDelegate MigrationDelegate { get; } = null;
        public ICrashMonitor CrashMonitor { get; }
    }


  
   

    

    

}