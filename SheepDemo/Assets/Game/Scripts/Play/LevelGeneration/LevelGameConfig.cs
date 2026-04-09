using System;
using UnityEngine;

/// <summary>
/// Data model for <c>*.game.json</c> levels (e.g. <c>level001.game.json</c>).
/// This mirrors the config schema so the generator can stay data-driven.
/// </summary>
[Serializable]
public class LevelGameConfig
{
    /// <summary>
    /// Config discriminator, e.g. "map-editor-game".
    /// </summary>
    public string kind;

    /// <summary>
    /// Schema version of the json.
    /// </summary>
    public int version;

    /// <summary>
    /// Grid width in cells (columns).
    /// </summary>
    public int width;

    /// <summary>
    /// Grid height in cells (rows).
    /// </summary>
    public int height;

    /// <summary>
    /// Entities placed on the grid.
    /// </summary>
    public InstanceData[] instances;

    [Serializable]
    public class InstanceData
    {
        /// <summary>
        /// Unique id within the level.
        /// </summary>
        public int id;

        /// <summary>
        /// Grid row index (convention decided by generator settings).
        /// </summary>
        public int row;

        /// <summary>
        /// Grid column index.
        /// </summary>
        public int col;

        /// <summary>
        /// Facing direction string, e.g. "left"/"up".
        /// </summary>
        public string direction;

        /// <summary>
        /// Type id used to select a prefab, e.g. "sheep".
        /// </summary>
        public string type;
    }

    /// <summary>
    /// Parse json text into a config object.
    /// Returns null on empty input.
    /// </summary>
    public static LevelGameConfig FromJson(string json)
    {
        return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<LevelGameConfig>(json);
    }
}

