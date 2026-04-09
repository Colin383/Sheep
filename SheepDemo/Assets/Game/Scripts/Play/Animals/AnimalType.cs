/// <summary>
/// Strong-typed animal identifiers used by runtime logic.
/// This is the authoritative list for matching level config "type" strings to prefabs.
/// </summary>
public enum AnimalType
{
    /// <summary>
    /// Fallback for invalid / unknown config values.
    /// Keep this as 0 so default(AnimalType) is safe.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Config type id: "sheep".
    /// </summary>
    Sheep = 1,
}

