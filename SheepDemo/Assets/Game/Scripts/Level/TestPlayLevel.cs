using UnityEngine;

public class TestPlayLevel : MonoBehaviour
{
    public LevelCtrl level;

    public CustomButton btn;

    public void Start()
    {
        level.Generate();
        btn.OnClick += (btn) =>
        {
            level.ClearSpawned();
            level.Generate();
        };
    }
}
