#if UNITY_EDITOR
using Game.Editor.UIGenerator;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>
    /// UIGenerator 保存 Prefab 后的项目定制后处理（通过 <see cref="IUIGeneratorPostProcessor"/> 自动接入管线）。
    /// <paramref name="prefabRoot"/> 为生成流程返回的根物体，当前实现一般为带 <see cref="Canvas"/> 的节点。
    /// </summary>
    public sealed class UIGeneratorGamePostProcessor : IUIGeneratorPostProcessor
    {
        /// <summary>数值越小越先执行；若再加其它 Processor，可用 100、200 排队。</summary>
        public int Order => 0;

        /// <summary>生成 UI 根 Canvas 的 <see cref="Canvas.sortingOrder"/>（需配合 <see cref="Canvas.overrideSorting"/>）。</summary>
        private const int DefaultCanvasSortingOrder = 100;

        public void OnPostProcessPrefab(GameObject prefabRoot, string prefabAssetPath, string sourceJsonPath)
        {
            if (prefabRoot == null)
            {
                return;
            }

            EnsureCanvasUtilityComponents(prefabRoot);
            EnsureClickVibrationOnCustomButtons(prefabRoot);
            UIGeneratorUIViewScriptGenerator.GenerateFromPrefabHierarchy(prefabRoot, prefabAssetPath);

            // 在此追加：替换字体、统一 Sorting、挂项目基类、按 JSON 名打 Tag 等。
            // 可仅用 prefabAssetPath / sourceJsonPath 做规则判断。
            ApplyProjectRules(prefabRoot, prefabAssetPath, sourceJsonPath);
        }

        private static void EnsureCanvasUtilityComponents(GameObject prefabRoot)
        {
            Canvas canvas = prefabRoot.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = prefabRoot.GetComponentInChildren<Canvas>(true);
            }

            if (canvas == null)
            {
                Debug.LogWarning($"[UIGeneratorGamePostProcessor] No Canvas under '{prefabRoot.name}', skip canvas setup.");
                return;
            }

            GameObject canvasGo = canvas.gameObject;

            // Canvas：渲染模式；Override Sorting（Inspector「Override Sorting / 覆盖排序」）开启后 sortingOrder 才独立参与排序
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;
            canvas.overrideSorting = true;
            canvas.sortingOrder = DefaultCanvasSortingOrder;

            // uGUI 射线检测（点击/拖拽）
            var raycaster = canvasGo.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = canvasGo.AddComponent<GraphicRaycaster>();
            }

            raycaster.ignoreReversedGraphics = true;
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.TwoD;
        }

        /// <summary>
        /// 与生成器内对 _btn 绑定 <see cref="ClickAudio"/> / <see cref="ClickScaleAnim"/> 一致，补上 <see cref="ClickVibration"/>（避免改 Bear 包源码）。
        /// </summary>
        private static void EnsureClickVibrationOnCustomButtons(GameObject prefabRoot)
        {
            CustomButton[] buttons = prefabRoot.GetComponentsInChildren<CustomButton>(true);
            foreach (CustomButton btn in buttons)
            {
                if (btn == null)
                {
                    continue;
                }

                if (btn.GetComponent<ClickVibration>() != null)
                {
                    continue;
                }

                btn.gameObject.AddComponent<ClickVibration>();
            }
        }

        private static void ApplyProjectRules(GameObject prefabRoot, string prefabAssetPath, string sourceJsonPath)
        {
            // 示例：按 sourceJsonPath Contains("Shop") 挂脚本、改 Canvas.sortingOrder 等。
        }
    }
}
#endif
