using CanindeWebServer.Data;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace CanindeWebServer.Services
{
    public class GenericCrudService
    {
        private readonly ProjetoDbContext _db;

        public GenericCrudService(ProjetoDbContext db)
        {
            _db = db;
        }

        private Type? GetEntityType(string entityName)
        {
            return _db.Model.GetEntityTypes()
                .Select(e => e.ClrType)
                .FirstOrDefault(t => string.Equals(t.Name, entityName, StringComparison.OrdinalIgnoreCase));
        }

        private IQueryable<object> GetQueryable(Type entityType)
        {
            var method = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)!.MakeGenericMethod(entityType);
            var dbSet = (IQueryable)method.Invoke(_db, null)!;
            return dbSet.Cast<object>();
        }

        private object GetDbSetInstance(Type entityType)
        {
            var method = typeof(DbContext).GetMethod("Set", Type.EmptyTypes)!.MakeGenericMethod(entityType);
            return method.Invoke(_db, null)!;
        }

        public async Task<(IEnumerable<object> items, int total)> GetAllAsync(string entityName, int page, int pageSize, string? search = null)
        {
            var entityType = GetEntityType(entityName)
                ?? throw new ArgumentException($"Entidade '{entityName}' não encontrada.");

            var query = GetQueryable(entityType);

            if (!string.IsNullOrEmpty(search))
            {
                var stringProps = entityType.GetProperties().Where(p => p.PropertyType == typeof(string)).ToList();
                if (stringProps.Any())
                {
                    // Build a simple OR filter using EF.Property and Expression trees would be ideal
                    // For simplicity use LINQ on client side (will pull all => not ideal for very large datasets)
                    // Better approach: build Expression with EF.Property and use Where - out of scope for now

                    query = query.Where(e => stringProps.Any(p => (EF.Property<string>(e, p.Name) ?? "").Contains(search)));
                }
            }

            int total = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return (items, total);
        }

        public async Task<object?> GetByIdAsync(string entityName, int id)
        {
            var entityType = GetEntityType(entityName)
                ?? throw new ArgumentException($"Entidade '{entityName}' não encontrada.");

            var dbSet = GetDbSetInstance(entityType);
            // FindAsync on generic DbSet
            var findAsync = dbSet.GetType().GetMethod("FindAsync", new Type[] { typeof(object[]) });
            if (findAsync != null)
            {
                var task = (System.Threading.Tasks.ValueTask<object?>)findAsync.Invoke(dbSet, new object[] { new object[] { id } })!;
                return await task;
            }

            // fallback
            return await Task.FromResult<object?>(null);
        }

        public async Task<object> AddAsync(string entityName, object model)
        {
            using var tx = await _db.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                if (entityName.StartsWith("Uvw", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Operação não permitida para views.");

                var entityType = GetEntityType(entityName)
                    ?? throw new ArgumentException($"Entidade '{entityName}' não encontrada.");

                // Use non-generic Add to avoid reflection AddAsync issues
                _db.Add(model);

                await _db.SaveChangesAsync().ConfigureAwait(false);

                await tx.CommitAsync().ConfigureAwait(false);

                return model;
            }
            catch (Exception)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task<object?> UpdateAsync(string entityName, int id, object model)
        {
            using var tx = await _db.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                if (entityName.StartsWith("Uvw", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Operação não permitida para views.");

                var entityType = GetEntityType(entityName)
                    ?? throw new ArgumentException($"Entidade '{entityName}' não encontrada.");

                var dbSet = GetDbSetInstance(entityType);
                var findAsync = dbSet.GetType().GetMethod("FindAsync", new Type[] { typeof(object[]) });
                object? existing = null;
                if (findAsync != null)
                {
                    var task = (System.Threading.Tasks.ValueTask<object?>)findAsync.Invoke(dbSet, new object[] { new object[] { id } })!;
                    existing = await task;
                }

                if (existing == null) return null;

                foreach (var prop in entityType.GetProperties().Where(p => p.CanWrite))
                {
                    var newValue = prop.GetValue(model);
                    prop.SetValue(existing, newValue);
                }

                _db.Update(existing);
                
                await _db.SaveChangesAsync().ConfigureAwait(false);

                await tx.CommitAsync().ConfigureAwait(false); 
                
                return existing;
            }
            catch (Exception)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string entityName, int id)
        {
            using var tx = await _db.Database.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                if (entityName.StartsWith("Uvw", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Operação não permitida para views.");

                var entityType = GetEntityType(entityName)
                    ?? throw new ArgumentException($"Entidade '{entityName}' não encontrada.");

                var dbSet = GetDbSetInstance(entityType);
                var findAsync = dbSet.GetType().GetMethod("FindAsync", new Type[] { typeof(object[]) });
                object? existing = null;
                if (findAsync != null)
                {
                    var task = (System.Threading.Tasks.ValueTask<object?>)findAsync.Invoke(dbSet, new object[] { new object[] { id } })!;
                    existing = await task;
                }

                if (existing == null) return false;

                _db.Remove(existing);

                await _db.SaveChangesAsync().ConfigureAwait(false);

                await tx.CommitAsync().ConfigureAwait(false);
                
                return true;
            }
            catch (Exception)
            {
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }
    }
}
