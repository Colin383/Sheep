using Bear.Fsm;

/// <summary>
/// Animal back state node.
/// </summary>
[StateMachineNode(typeof(BaseAnimal), AnimalStateName.BACK, false)]
public class Animal_Back : StateNode
{
    public override void OnEnter()
    {
        
    }

    public override void OnUpdate()
    {
        
    }

    public override void OnExit()
    {
  
    }
}
