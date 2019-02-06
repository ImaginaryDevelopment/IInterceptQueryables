using System.Linq;

namespace QueryInterception
{
    public interface IRepository<out T>
    {
        IQueryable<T> Query();
    }
}