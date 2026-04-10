using Bear.Fsm;

/// <summary>
/// Animal idle state node.
/// </summary>
[StateMachineNode(typeof(BaseAnimal), AnimalStateName.IDLE, true)]
public class Animal_Idle : StateNode
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
