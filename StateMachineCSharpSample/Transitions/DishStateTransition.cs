internal class DishStateTransition
{
    private readonly DishStates CurrentState;
    private readonly DishCommands Command;
    public readonly DishConditions? Condition;

    public DishStateTransition(DishStates currentState, DishCommands command, DishConditions? condition)
    {
        CurrentState = currentState;
        Command = command;
        Condition = condition;
    }

    public override int GetHashCode()
    {
        return 17 + 37 * CurrentState.GetHashCode() + 73 * Command.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        DishStateTransition other = obj as DishStateTransition;
        return other != null && this.CurrentState == other.CurrentState && this.Command == other.Command;
    }
}