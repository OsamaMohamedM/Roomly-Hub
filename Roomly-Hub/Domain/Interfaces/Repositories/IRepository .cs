namespace Domain.Interfaces.Repositories
{
    internal interface IRepository<T> : IDisposable where T : class
    {
        T GetAsync(int id);

        IEnumerable<T> GetAllAsync();

        void AddAsync(T entity);

        void UpdateAsync(T entity);

        void DeleteByIdAsync(int id);

        void DeleteAllAsync();

        void SaveChangesAsync();
    }
}