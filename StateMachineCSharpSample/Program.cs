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
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommands.Cooking, p.OrderMove(OrderCommands.Cooking));

        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommands.Complete, p.OrderMove(OrderCommands.Complete)); // here we are told that not all the dishes ready

        // continue cooking dishes
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Dish", DishCommands.Cooking, p.DishMove(DishCommands.Cooking));
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Dish", DishCommands.Complete, p.DishMove(DishCommands.Complete));

        // try to complete one more
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommands.Complete, p.OrderMove(OrderCommands.Complete));
        // failed

        // cooking one more dish
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Dish", DishCommands.Cooking, p.DishMove(DishCommands.Cooking));
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Dish", DishCommands.Complete, p.DishMove(DishCommands.Complete));

        // try to complete
        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommands.Complete, p.OrderMove(OrderCommands.Complete));

        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommands.ToShipping, p.OrderMove(OrderCommands.ToShipping));
        // failed to handle shipping

        Console.WriteLine("Object: {0}, command: {1}, result: {2}", "Order", OrderCommands.ToHallDelivered, p.OrderMove(OrderCommands.ToHallDelivered));
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

    private Dictionary<OrderStateTransition, OrderStates> orderTransitions;
    private Dictionary<DishStateTransition, DishStates> dishTransitions;
    private Order order;

    public Process(Order order)
    {
        this.order = order;
        order.State = order.Time == null ? OrderStates.New : OrderStates.InQueue;

        foreach (var dish in order.Dishes)
        {
            dish.State = DishStates.New;
        }

        //filling dictionaries of transitions for meals and orders
        orderTransitions = new Dictionary<OrderStateTransition, OrderStates>
        {
            { new OrderStateTransition(OrderStates.InQueue, OrderCommands.ToNew, OrderConditions.TimeHasCome), OrderStates.New },
            { new OrderStateTransition(OrderStates.New, OrderCommands.Cooking, null), OrderStates.Cooking },
            { new OrderStateTransition(OrderStates.Cooking, OrderCommands.Complete, OrderConditions.AllDishesArePrepared), OrderStates.Complete },
            { new OrderStateTransition(OrderStates.Complete, OrderCommands.ToShipping, OrderConditions.ReadyForShippingOrHall), OrderStates.Shipping },
            { new OrderStateTransition(OrderStates.Shipping, OrderCommands.ToShippingDelivered, null), OrderStates.DeliveredToShippingAddress },
            { new OrderStateTransition(OrderStates.Complete, OrderCommands.ToHallDelivered, OrderConditions.ReadyForShippingOrHall), OrderStates.DeliveredToHall }
        };

        dishTransitions = new Dictionary<DishStateTransition, DishStates>
        {
            {new DishStateTransition(DishStates.InQueue, DishCommands.ToNew, DishConditions.TimeHasCome), DishStates.New },
            {new DishStateTransition(DishStates.New, DishCommands.Cooking, null), DishStates.Cooking },
            {new DishStateTransition(DishStates.Cooking, DishCommands.Complete, null), DishStates.Ready },
        };
    }

    public string OrderMove(OrderCommands command)
    {
        OrderStateTransition transition = new OrderStateTransition(order.State, command, null);
        OrderStates nextState;
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
                    if (nextState == OrderStates.Shipping && order.Destination == DestinationRoute.Shipping
                        || nextState == OrderStates.DeliveredToHall && order.Destination == DestinationRoute.ToHall
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

    public string DishMove(DishCommands command)
    {
        var dish = order.Dishes.Where(x => x.State != DishStates.Ready).FirstOrDefault();

        if (dish != null)
        {
            DishStateTransition transition = new DishStateTransition(dish.State, command, null);
            DishStates nextState;
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