using System;
using System.Collections.Generic;
using System.Linq;

public class Order
{
    public DestinationRoute Destination { get; set; }
    public List<Dish> Dishes { get; set; }
    public OrderStates State { get; set; }
    public DateTime? Time { get; set; }

    public bool IsComplete
    {
        get
        {
            return Dishes.Select(x => x.State == DishStates.Ready).Aggregate(true, (a, b) => (a && b));
        }
    }
}