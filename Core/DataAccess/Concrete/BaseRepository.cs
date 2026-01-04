using Core.DataAccess.Abstract;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Core.DataAccess.Concrete
{

    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public IQueryable<T> Query() => _dbSet.AsQueryable();

        public async Task<T?> GetByIdAsync(int id)
            => await _dbSet.FindAsync(id);

        public async Task AddAsync(T entity)
            => await _dbSet.AddAsync(entity);

        public void Update(T entity)
            => _dbSet.Update(entity);

        public void Delete(T entity)
            => _dbSet.Remove(entity);
        //public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

        //public Task UpdateAsync(T entity)
        //{
        //    _dbSet.Update(entity);
        //    return Task.CompletedTask;
        //}

        //public Task DeleteAsync(T entity)
        //{
        //    entity.Deleted = 1;
        //    entity.LastUpdatedDate = DateTime.Now;
        //    return Task.CompletedTask;
        //}

        //public async Task<T?> GetByIdAsync(int id) =>
        //    await _dbSet.FirstOrDefaultAsync(x => x.Id == id && x.Deleted == 0);

        //public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null)
        //{
        //    IQueryable<T> query = _dbSet.Where(x => x.Deleted == 0);
        //    if (filter != null) query = query.Where(filter);
        //    return await query.ToListAsync();
        //}
    }
}

