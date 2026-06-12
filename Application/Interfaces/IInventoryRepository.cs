using System.Data.Common;
using Scenarios.Domain.Entities;

namespace Scenarios.Application.Interfaces;

public interface IInventoryRepository
{
    Task<int> GetQuantityAsync(string productName, DbConnection conn, DbTransaction? tx = null);
    Task DecrementQuantityAsync(string productName, DbConnection conn, DbTransaction? tx = null);
    Task<IReadOnlyList<Inventory>> GetAllAsync(DbConnection conn, DbTransaction? tx = null);
}
