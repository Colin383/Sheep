using System;
using System.Collections.Generic;
using System.Threading;
using Guru.SDK.Framework.Analytics.Appsflyer;
using Guru.SDK.Framework.Core.Analytics.Strategy;
using Guru.SDK.Framework.Core.Analytics.Transmitter;
using Guru.SDK.Framework.Core.Spec.Protocols.Analytics;

namespace Game
{
    public class GameAnalytics  : IAnalyticsDelegate
    {
        private static readonly Lazy<GameAnalytics> LazyInstance =
            new(() => new GameAnalytics(), LazyThreadSafetyMode.ExecutionAndPublication);
     
        public static GameAnalytics Instance => LazyInstance.Value;
        
        
        public IReadOnlyList<IAnalyticsTransmitter> CustomTransmitters => _customTransmitters;
        public Dictionary<string, StrategyRule> ExplicitRules { get; }
        public List<StrategyRule> PriorityRules { get; }
        
        
        private readonly List<IAnalyticsTransmitter> _customTransmitters;
        
        private GameAnalytics()
        {
            // 实现 Appsflyer 打点协议
            _customTransmitters = new List<IAnalyticsTransmitter>
            {
                GuruAppsflyer.Prepare(CreateAppsFlyerConfig()),
            };
            
        }
        
        // Appsflyer 买量点扩展
        private AppsFlyerConfig CreateAppsFlyerConfig()
        {
            const string afDevKey = "JGsA3FFVn5q2jGvJJScebV";  // GuruGame 统一的 Key
            const string appleId = "6757749388";        // Game AppleId 需要到 AppleConnect 后台查看
            
            var strategy = AppsFlyerStrategy.Create(new List<AppsflyerEventDefinition>()
            {
                AppsflyerEventDefinition.Define("iap_purchase", AppsflyerEventRule.Revenue),
                AppsflyerEventDefinition.Define("sub_purchase", AppsflyerEventRule.Revenue),
                AppsflyerEventDefinition.Define("iap_ret_true"),
                // =================================================== 自定义数据
                AppsflyerEventDefinition.Define("level_end_success_1"),
                AppsflyerEventDefinition.Define("level_end_success_2"),
                AppsflyerEventDefinition.Define("level_end_success_3"),
                AppsflyerEventDefinition.Define("level_end_success_4"),
                AppsflyerEventDefinition.Define("level_end_success_5"),
                AppsflyerEventDefinition.Define("level_end_success_7"),
                AppsflyerEventDefinition.Define("level_end_success_9"),
                AppsflyerEventDefinition.Define("level_end_success_11"),
                AppsflyerEventDefinition.Define("level_end_success_13"),
                AppsflyerEventDefinition.Define("level_end_success_15"),
                AppsflyerEventDefinition.Define("level_end_success_20"),
                AppsflyerEventDefinition.Define("level_end_success_25"),
                AppsflyerEventDefinition.Define("level_end_success_30"),
                AppsflyerEventDefinition.Define("level_end_success_35"),
                AppsflyerEventDefinition.Define("level_end_success_36"),
                
            });
            return new AppsFlyerConfig(afDevKey, appleId, strategy);
        }
        
        
    }
}