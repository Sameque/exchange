using OrderGenerator.Domain.Entities;

namespace OrderGenerator.Domain.Interfaces;

public interface IFixApplication
{
    Task<Order> SendOrder(Order order);
    bool Connected { get; }
}