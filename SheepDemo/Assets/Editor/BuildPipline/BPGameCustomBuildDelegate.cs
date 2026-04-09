using System;
using GF;
using Guru.Editor;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

/// <summary>
/// BrainPunk 项目组自定义构建管线
/// </summary>
public class BPGameCustomBuildDelegate: IAppBuildDelegate
{
    private static BPGameCustomBuildDelegate _instance;
    public static BPGameCustomBuildDelegate Instance => _instance ??= new BPGameCustomBuildDelegate();
    
    public void OnBeforeBuildStart(BuildTarget buildTarget)
    {
        Debug.Log("======== Project Asset Process Start ========");
        ChangeGameSetting();
        AssetDatabase.Refresh();
        RefreshPackageAssets(buildTarget);
        Debug.Log("======== Project Asset Process End ========");
    }


    public void OnSetBuildPlayerOptions(BuildTarget buildTarget, BuildPlayerOptions buildPlayerOptions)
    {
        // 构建参数设置
        var assetBundleManifestPath =  buildPlayerOptions.assetBundleManifestPath;
        if (string.IsNullOrEmpty(assetBundleManifestPath))
        {
            return;
        }

        // TODO: 执行 Bundle 构建逻辑
        if (buildTarget == BuildTarget.Android)
        {
            
        }
        else if (buildTarget == BuildTarget.iOS)
        {
            
        }

    }

    public void SaveBuildNumberAndCode(BuildTarget buildTarget, string buildVersion, string buildCode)
    {
        Debug.Log($"[{buildTarget}] Current Build Version: {buildVersion}-{buildCode}");
        // TODO: 项目组可自行保存版本信息
    }

    public void OnBuildSuccess(BuildTarget buildTarget, string outputFolder)
    {
        Debug.Log($"[{buildTarget}] Build Success: {outputFolder}");
        // TODO:  项目组处理后继步骤：打包成功
    }

    public void OnBuildFailed(BuildTarget buildTarget, Exception ex)
    {
        Debug.Log($"[{buildTarget}] Build  Failed: {ex}");
        // TODO:  项目组处理后继步骤：打包失败
    }
    
        
    private static void ChangeGameSetting()
    {
        var gameSettings = AssetDatabase.LoadAssetAtPath<GameSetting>("Assets/Resources/GameSetting.asset");
        for (int i = 0; i < gameSettings.builtinPackageList.Count; i++)
        {
            gameSettings.builtinPackageList[i].playMode = EPlayMode.OfflinePlayMode;
        }
        EditorUtility.SetDirty(gameSettings);
        AssetDatabase.SaveAssetIfDirty(gameSettings);
    }

    private static void RefreshPackageAssets(BuildTarget buildTarget)
    {
        var bundleBuilder = new ScriptableBuildPipeline();
        int currentTimeStamp = GetCurrentTimeStamp();
        var buildParameters = new ScriptableBuildParameters()
        {
            CompressOption = ECompressOption.LZ4,
            BuildOutputRoot = AssetBundleBuilderHelper.GetDefaultBuildOutputRoot(),
            BuildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot(),
            BuildTarget = buildTarget,
            BuildMode = EBuildMode.IncrementalBuild,
            PackageName = "GamePackage",
            PackageVersion = currentTimeStamp.ToString(),
            FileNameStyle = EFileNameStyle.HashName,
            BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
            EncryptionServices = new EncryptoHTXOR()
        };
        bundleBuilder.Run(buildParameters, false);
    }

    private static int GetCurrentTimeStamp()
    {
        int tickToMillisecond = 10000;
        long stamp197011 = 62135596800000;
        int timeStamp = (int)(DateTime.UtcNow.Ticks / tickToMillisecond - stamp197011);
        return timeStamp;
    }
}
