using Newtonsoft.Json;

namespace Game.Editor.UIGenerator
{
    [System.Serializable]
    public class LayerData
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("textInfo")]
        public TextInfoData TextInfo { get; set; }

        /// <summary>切图文件名或相对路径（与图层名解耦）；优先用于 Sprite 检索。</summary>
        [JsonProperty("src")]
        public string Src { get; set; }

        public bool IsTextLayer => Type == "textLayer";
        public bool IsShapeLayer => Type == "shapeLayer";
        public bool IsImageLayer => Type == "layer";
    }

    [System.Serializable]
    public class TextInfoData
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("orientation")]
        public string Orientation { get; set; }

        [JsonProperty("bounds")]
        public BoundsData Bounds { get; set; }

        [JsonProperty("fontPostScriptName")]
        public string FontPostScriptName { get; set; }

        [JsonProperty("fontName")]
        public string FontName { get; set; }

        [JsonProperty("fontSize")]
        public float FontSize { get; set; }

        [JsonProperty("bold")]
        public bool Bold { get; set; }

        [JsonProperty("italic")]
        public bool Italic { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        /// <summary>不透明度 0–100；JSON 未提供时为 null，仅用 color 中的 alpha（若有）。</summary>
        [JsonProperty("opacity")]
        public float? Opacity { get; set; }
    }
}

