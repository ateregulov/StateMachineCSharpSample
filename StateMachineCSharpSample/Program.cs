using System;
using System.Collections.Generic;
using System.Linq;

public class Program
{
    static void Main(string[] args)
    {
        var order = GetTestOrder();
        Process p = new Process(order);

        Console.WriteLine("Init state = " + p.GetCurrentState);
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommand.Cooking, p.OrderMove(OrderCommand.Cooking));


        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommand.Complete, p.OrderMove(OrderCommand.Complete)); // here we are told that not all the dishes ready

        // continue cooking dishes
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Dish", DishCommand.Cooking, p.DishMove(DishCommand.Cooking));
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Dish", DishCommand.Complete, p.DishMove(DishCommand.Complete));

        // try to complete one more
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommand.Complete, p.OrderMove(OrderCommand.Complete));
        // failed

        // cooking one more dish
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Dish", DishCommand.Cooking, p.DishMove(DishCommand.Cooking));
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Dish", DishCommand.Complete, p.DishMove(DishCommand.Complete));


        // try to complete 
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommand.Complete, p.OrderMove(OrderCommand.Complete));

        
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommand.ToShipping, p.OrderMove(OrderCommand.ToShipping));
        // failed to handle shipping
                
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommand.ToHallDelivered, p.OrderMove(OrderCommand.ToHallDelivered));
        // then successfully pass the ordet to hall

        Console.ReadLine();
    }

    static Order GetTestOrder()
    {
        return new Order()
        {
            Destination = DestinationRoute.ToHall,
            Dishes = new List<Dish>() {
                new Dish(),
                new Dish(),
            }
        };
    }
}



public class Process
{
    // initial state, valid commands and the transitions conditions
    class OrderStateTransition
    {
        readonly OrderState CurrentState;
        readonly OrderCommand Command;
        public readonly OrderCondition? Condition;

        public OrderStateTransition(OrderState currentState, OrderCommand command, OrderCondition? condition)
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

    class DishStateTransition
    {
        readonly DishState CurrentState;
        readonly DishCommand Command;
        public readonly DishCondition? Condition;

        public DishStateTransition(DishState currentState, DishCommand command, DishCondition? condition)
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



    Dictionary<OrderStateTransition, OrderState> orderTransitions;
    Dictionary<DishStateTransition, DishState> dishTransitions;
    Order order;

    public Process(Order order)
    {
        this.order = order;
        order.State = order.Time == null ? OrderState.New : OrderState.InQueue;

        foreach (var dish in order.Dishes)
        {
            dish.State = DishState.New;
        }

        //filling dictionaries of transitions for meals and orders

        orderTransitions = new Dictionary<OrderStateTransition, OrderState>
        {
            { new OrderStateTransition(OrderState.InQueue, OrderCommand.ToNew, OrderCondition.TimeHasCome), OrderState.New },
            { new OrderStateTransition(OrderState.New, OrderCommand.Cooking, null), OrderState.Cooking },
            { new OrderStateTransition(OrderState.Cooking, OrderCommand.Complete, OrderCondition.AllDishesArePrepared), OrderState.Complete },
            { new OrderStateTransition(OrderState.Complete, OrderCommand.ToShipping, OrderCondition.ReadyForShippingOrHall), OrderState.Shipping },
            { new OrderStateTransition(OrderState.Shipping, OrderCommand.ToShippingDelivered, null), OrderState.DeliveredToShippingAddress },
            { new OrderStateTransition(OrderState.Complete, OrderCommand.ToHallDelivered, OrderCondition.ReadyForShippingOrHall), OrderState.DeliveredToHall }
        };

        dishTransitions = new Dictionary<DishStateTransition, DishState>
        {
            {new DishStateTransition(DishState.InQueue, DishCommand.ToNew, DishCondition.TimeHasCome), DishState.New },
            {new DishStateTransition(DishState.New, DishCommand.Cooking, null), DishState.Cooking },
            {new DishStateTransition(DishState.Cooking, DishCommand.Complete, null), DishState.Ready },
        };

    }

    public string OrderMove(OrderCommand command)
    {
        OrderStateTransition transition = new OrderStateTransition(order.State, command, null);
        OrderState nextState;
        if (!orderTransitions.TryGetValue(transition, out nextState))
        {
            return "Incorrect transition: " + order.State + " -> " + command;
        }

        string message = "";

        // extracted condition suitable for pattern transfer
        var key = orderTransitions.Keys.Where(x => x.Equals(transition)).FirstOrDefault();
        var condition = key.Condition;

        if (condition != null)
        {
            //verify the conditions of the transitions
            switch (condition.ToString())
            {
                case "TimeHasCome":
                    // unrealized then check in time, at the moment emulated the true
                    order.State = nextState;
                    break;

                case "AllDishesArePrepared":
                    if (order.IsComplete)
                    {
                        order.State = nextState;
                    }
                    else
                    {
                        message = "- Not all dishes are complete";
                        nextState = order.State;
                    }
                    break;

                case "ReadyForShippingOrHall":
                    if (nextState == OrderState.Shipping && order.Destination == DestinationRoute.Shipping
                        || nextState == OrderState.DeliveredToHall && order.Destination == DestinationRoute.ToHall
                        )
                    {
                        order.State = nextState;
                    }
                    else
                    {
                        message = "- wrong direction";
                        nextState = order.State;
                    }

                    break;
                default:
                    break;
            }
        }
        else
        {
            order.State = nextState;
        }






        return nextState.ToString() + message;
    }

    public string DishMove(DishCommand command)
    {
        var dish = order.Dishes.Where(x => x.State != DishState.Ready).FirstOrDefault();

        if (dish != null)
        {

            DishStateTransition transition = new DishStateTransition(dish.State, command, null);
            DishState nextState;
            if (!dishTransitions.TryGetValue(transition, out nextState))
            {
                return "Wrong direction: " + dish.State + " -> " + command;
            }

            var condition = dishTransitions.Keys.Where(x => x.Equals(transition)).First().Condition;

            if (condition != null)
            {

                switch (condition.ToString())
                {
                    case "TimeHasCome":
                        // unrealized then check in time, at the moment emulated the true
                        dish.State = nextState;

                        break;


                    default:
                        break;
                }
            }
            else
            {
                dish.State = nextState;
            }

            return nextState.ToString();
        }
        else
        {
            return "All dishes are ready";
        }
    }


    public string GetCurrentState
    {
        get
        {
            return order.State.ToString();
        }
    }
}



public enum OrderState
{
    InQueue,
    New,
    Cooking,
    Complete,
    Shipping,
    DeliveredToHall,
    DeliveredToShippingAddress
}

public enum DishState
{
    InQueue,
    New,
    Cooking,
    Ready
}

public enum OrderCommand
{
    ToQueue,
    ToNew,
    Cooking,
    Complete,
    ToShipping,
    ToShippingDelivered,
    ToHallDelivered
}

public enum DishCommand
{
    ToQueue,
    ToNew,
    Cooking,
    Complete
}

public enum DestinationRoute
{
    Shipping,
    ToHall
}

public enum OrderCondition
{
    TimeHasCome,
    AllDishesArePrepared,
    ReadyForShippingOrHall
}

public enum DishCondition
{
    TimeHasCome,
}

public class Order
{
    public DestinationRoute Destination { get; set; }
    public List<Dish> Dishes { get; set; }
    public OrderState State { get; set; }
    public DateTime? Time { get; set; }
    public bool IsComplete
    {
        get
        {
            return Dishes.Select(x => x.State == DishState.Ready).Aggregate(true, (a, b) => (a && b));
        }
    }
}

public class Dish
{
    public DishState State { get; set; }
}






