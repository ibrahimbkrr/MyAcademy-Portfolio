using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyAcademy_PortfolioProject.Entities;

namespace MyAcademy_PortfolioProject.Tests;

public sealed class SaveFailureInterceptor : SaveChangesInterceptor
{
    public bool FailAboutWrites { get; set; }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken=default)
    {
        if(FailAboutWrites && eventData.Context!.ChangeTracker.Entries<About>().Any(x=>x.State is EntityState.Added or EntityState.Modified))
            throw new DbUpdateException("Simulated test save failure");
        return base.SavingChangesAsync(eventData,result,cancellationToken);
    }
}
