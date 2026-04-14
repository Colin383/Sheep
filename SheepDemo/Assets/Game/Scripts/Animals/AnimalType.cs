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

    /// <summary>
    /// 爆炸羊 config type id  "bombsheep" 
    /// </summary>
    BombSheep = 2,

    /// <summary>
    /// 鸡 "chick"
    /// </summary>
    Chick = 3,

    /// <summary>
    /// 大象 "elephant"
    /// </summary>
    Elephant = 4,

    /// <summary>
    /// 倒计时 羊
    /// </summary>
    CdSheep = 5,    
}

