namespace Scenarios.Domain.Entities;

public sealed record Order(int Id, int UserId, decimal Amount, string Status, DateTime CreatedAt);
