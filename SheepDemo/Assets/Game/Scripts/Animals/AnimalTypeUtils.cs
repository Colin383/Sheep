/// <summary>
/// Converts between level-config string type ids (e.g. "sheep") and <see cref="AnimalType"/>.
/// Keep this mapping centralized so config changes don't leak into gameplay code.
/// </summary>
public static class AnimalTypeUtils
{
    /// <summary>
    /// Parse config "type" field into a strong type.
    /// Returns <see cref="AnimalType.Unknown"/> for null/empty/unsupported values.
    /// </summary>
    public static AnimalType Parse(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            return AnimalType.Unknown;
        }

        switch (type.Trim().ToLowerInvariant())
        {
            case "sheep":
                return AnimalType.Sheep;
            case "bombsheep":
                return AnimalType.BombSheep;
            case "chick":
                return AnimalType.Chick;
            case "elephant":
                return AnimalType.Elephant;
            case "cdsheep":
                return AnimalType.CdSheep;
            default:
                return AnimalType.Unknown;
        }
    }

    /// <summary>
    /// Converts a runtime type back into its config string id.
    /// Returns empty string for <see cref="AnimalType.Unknown"/>.
    /// </summary>
    public static string ToConfigTypeId(this AnimalType type)
    {
        return type switch
        {
            AnimalType.Sheep => "sheep",
            AnimalType.BombSheep => "bombsheep",
            AnimalType.Chick => "chick",
            AnimalType.Elephant => "elephant",
            AnimalType.CdSheep => "cdsheep",
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Convenience matcher for "does this runtime type equal the config type string".
    /// Unknown is treated as non-match to avoid silently accepting invalid config.
    /// </summary>
    public static bool IsMatchConfigType(this AnimalType type, string configType)
    {
        var parsed = Parse(configType);
        return type == parsed && parsed != AnimalType.Unknown;
    }
}

