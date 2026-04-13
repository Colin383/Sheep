using System.Collections.Generic;
using Game.Play;
using UnityEngine;

public partial class LevelCtrl
{
    private readonly List<Chick> chicks = new();

    #region Click Trigger 

    /// <summary>
    /// Click Trigger 触发时，获取 transform BaseAnimal，触发 OnClickTrigger 事件
    /// </summary>
    /// <param name="transform"></param>
    public void OnClickAnimal(Transform transform)
    {
        if (transform == null)
            return;

        if (PlayCtrl.Instance == null || !PlayCtrl.Instance.CheckState(GamePlayStateName.PLAYING))
            return;

        var animal = transform.GetComponent<BaseAnimal>();
        if (animal == null)
            animal = transform.GetComponentInParent<BaseAnimal>();

        if (animal != null)
            animal.OnClickTrigger();

        for (int i = 0; i < chicks.Count; i++)
        {
            var chick = chicks[i];
            if (chick == null)
                continue;

            chick.TryMoving();
        }
    }

    #endregion

}
