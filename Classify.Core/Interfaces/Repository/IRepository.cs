namespace Classify.Core.Interfaces.Repository;

/// <summary>
/// Common repository functions
/// </summary>
/// <typeparam name="T">The domain type of the repository</typeparam>
public interface IRepository<T>
{
    public Task<T?> GetByIdAsync(int id);
    public Task<IEnumerable<T>> GetAllAsync();

    public Task<T> AddAsync(T entity);

    public void Update(T entity);

    public Task DeleteAsync(int id);
    // Additional functions may be added later!
    public Task<bool> AnyAsync();
}