using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Guru.SDK.Framework.Ads.Max;
using Guru.SDK.Framework.Core.Spec.Protocols.Ads;

namespace Game
{
    public class GameAdsSpec  : IAdsSpecDelegate
    {
        private static readonly Lazy<GameAdsSpec> LazyInstance =
            new(() => new GameAdsSpec(), LazyThreadSafetyMode.ExecutionAndPublication);
        
        public static GameAdsSpec Instance => LazyInstance.Value;
        
        public UniTask<GuruAdsSdk> BuildAdsSdk()
        {
            var guruAds = MaxGuruAds.Instance;
            return new UniTask<GuruAdsSdk>(guruAds);
        }
    }

}