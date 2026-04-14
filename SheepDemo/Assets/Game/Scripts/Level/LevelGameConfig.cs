using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data model for level config json.
/// Supports both the legacy <c>instances</c> format and the new <c>elements + placements</c> format.
/// </summary>
[Serializable]
public class LevelGameConfig
{
    /// <summary>
    /// Config discriminator, e.g. "map-editor-game".
    /// </summary>
    public string kind;

    /// <summary>
    /// Level name.
    /// </summary>
    public string name;

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

    #region Legacy format

    /// <summary>
    /// Entities placed on the grid (legacy direct format).
    /// </summary>
    public InstanceData[] instances;

    #endregion

    #region New format

    /// <summary>
    /// Element templates defined in the level.
    /// </summary>
    public ElementData[] elements;

    /// <summary>
    /// Placements referencing elements.
    /// </summary>
    public PlacementData[] placements;

    [Serializable]
    public class ElementData
    {
        public string id;
        public string name;
        public string type;
        public int gridN;
        public string image;
        public string color;
        public string direction;
        public bool hasParam;
    }

    [Serializable]
    public class PlacementData
    {
        public string elementId;
        public int row;
        public int col;
        public int id;
        public int rotation;
        public string direction;
        public string param;
    }

    #endregion

    [Serializable]
    public class InstanceData
    {
        /// <summary>
        /// Unique id within the level.
        /// </summary>
        public int id;

        /// <summary>
        /// Grid row index.
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

        /// <summary>
        /// Extra param for animal-specific parsing.
        /// </summary>
        public string param;
    }

    /// <summary>
    /// Parse json text into a config object.
    /// Automatically resolves <c>elements + placements</c> into <c>instances</c> when needed.
    /// Returns null on empty input.
    /// </summary>
    public static LevelGameConfig FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        var config = JsonUtility.FromJson<LevelGameConfig>(json);
        if (config == null)
            return null;

        // If new format is present but legacy instances are missing, resolve them.
        if ((config.instances == null || config.instances.Length == 0)
            && config.placements != null && config.placements.Length > 0)
        {
            config.ResolvePlacements();
        }

        return config;
    }

    private void ResolvePlacements()
    {
        var elementById = new Dictionary<string, ElementData>();
        if (elements != null)
        {
            foreach (var element in elements)
            {
                if (!string.IsNullOrEmpty(element.id))
                    elementById[element.id] = element;
            }
        }

        var resolved = new List<InstanceData>(placements.Length);
        foreach (var placement in placements)
        {
            string type = string.Empty;
            string direction = placement.direction;

            if (elementById.TryGetValue(placement.elementId, out var element))
            {
                type = element.type ?? string.Empty;
                if (string.IsNullOrWhiteSpace(direction))
                    direction = element.direction;
            }

            resolved.Add(new InstanceData
            {
                id = placement.id,
                row = placement.row,
                col = placement.col,
                direction = direction ?? string.Empty,
                type = type,
                param = placement.param ?? string.Empty
            });
        }

        instances = resolved.ToArray();
    }
}
