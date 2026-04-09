using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Editor.UIGenerator
{
    /// <summary>
    /// Photoshop / 设计工具导出的 document + layers（含 isGroup / children）结构。
    /// </summary>
    [System.Serializable]
    public class PsdExportRoot
    {
        [JsonProperty("document")]
        public PsdDocumentInfo Document { get; set; }

        [JsonProperty("layers")]
        public List<PsdLayerNode> Layers { get; set; }
    }

    [System.Serializable]
    public class PsdDocumentInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("resolution")]
        public float Resolution { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        /// <summary>json+polyfill 等新导出脚本附带的元信息（可选）。</summary>
        [JsonProperty("exportMeta")]
        public PsdExportMeta ExportMeta { get; set; }
    }

    [System.Serializable]
    public class PsdExportMeta
    {
        [JsonProperty("format")]
        public string Format { get; set; }

        [JsonProperty("scriptVersion")]
        public string ScriptVersion { get; set; }
    }

    /// <summary>图层填充色 / 文本颜色（新格式为 r,g,b,hex）。</summary>
    [System.Serializable]
    public class PsdLayerColor
    {
        [JsonProperty("hex")]
        public string Hex { get; set; }

        [JsonProperty("r")]
        public int R { get; set; }

        [JsonProperty("g")]
        public int G { get; set; }

        [JsonProperty("b")]
        public int B { get; set; }
    }

    [System.Serializable]
    public class PsdLayerNode
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }

        [JsonProperty("opacity")]
        public float Opacity { get; set; }

        [JsonProperty("isGroup")]
        public bool IsGroup { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("bounds")]
        public PsdBounds Bounds { get; set; }

        [JsonProperty("children")]
        public List<PsdLayerNode> Children { get; set; }

        [JsonProperty("color")]
        public PsdLayerColor LayerColor { get; set; }

        /// <summary>文本图层实际文案（新导出）。</summary>
        [JsonProperty("content")]
        public string Content { get; set; }

        /// <summary>文本字号（点，与 PS 一致；新导出）。</summary>
        [JsonProperty("fontSize")]
        public float FontSize { get; set; }

        /// <summary>切图文件名或相对路径（新导出，与图层名解耦；多图层可共用同一 src）。</summary>
        [JsonProperty("src")]
        public string Src { get; set; }
    }

    [System.Serializable]
    public class PsdBounds
    {
        [JsonProperty("left")]
        public float Left { get; set; }

        [JsonProperty("top")]
        public float Top { get; set; }

        [JsonProperty("right")]
        public float Right { get; set; }

        [JsonProperty("bottom")]
        public float Bottom { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }
    }
}
