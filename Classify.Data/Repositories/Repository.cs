using Classify.Core.Interfaces.Repository;
using Classify.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Classify.Data.Repositories;

public class Repository<T>(ClassifyContext context) : IRepository<T>
    where T : class
{
    protected readonly ClassifyContext Context = context;
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(int id)
        => await DbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await DbSet.AsNoTracking().ToListAsync();

    public async Task<T> AddAsync(T entity)
    {
        await DbSet.AddAsync(entity);
        return entity;
    }

    public void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public async Task DeleteAsync(int id)
    {
        T? entity = await GetByIdAsync(id);
        if (entity != null)
        {
            DbSet.Remove(entity);
        }
    }

    public async Task<bool> AnyAsync()
    {
        return await DbSet.AnyAsync();
    }
}