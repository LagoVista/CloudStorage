using Microsoft.EntityFrameworkCore;
using System.Linq;

public static class DbSetQueryExtensions
{
    public static IQueryable<T> ReadonlyQuery<T>(this DbSet<T> set)
        where T : class
        => set.AsNoTracking();
}