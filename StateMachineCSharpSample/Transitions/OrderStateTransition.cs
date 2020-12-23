internal class OrderStateTransition
{
    private readonly OrderStates CurrentState;
    private readonly OrderCommands Command;
    public readonly OrderConditions? Condition;

    public OrderStateTransition(OrderStates currentState, OrderCommands command, OrderConditions? condition)
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
        OrderStateTransition other = obj as OrderStateTransition;
        return other != null && this.CurrentState == other.CurrentState && this.Command == other.Command;
    }
}