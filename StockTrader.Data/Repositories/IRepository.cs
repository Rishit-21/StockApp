using System.Linq.Expressions;

namespace StockTrader.Data.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity); // Typically synchronous as it only marks state
    void Remove(T entity); // Typically synchronous
    void RemoveRange(IEnumerable<T> entities); // Typically synchronous
    Task<int> SaveChangesAsync(); // To commit transactions
}
