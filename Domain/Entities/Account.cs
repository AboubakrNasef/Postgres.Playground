namespace Scenarios.Domain.Entities;

public sealed record Account(int Id, string Name, decimal Balance, int Version);
