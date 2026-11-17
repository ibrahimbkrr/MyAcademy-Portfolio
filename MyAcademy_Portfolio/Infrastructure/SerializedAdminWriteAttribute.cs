using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyAcademy_PortfolioProject.Context;

namespace MyAcademy_PortfolioProject.Infrastructure;

/// <summary>SQL Server session-owned application lock across instances, before read/insert.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SerializedAdminWriteAttribute(string resource) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<PortfolioContext>();
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method) || !db.Database.IsSqlServer())
        {
            await next();
            return;
        }
        await db.Database.OpenConnectionAsync(context.HttpContext.RequestAborted);
        var acquired = false;
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock @Resource={"Portfolio.Admin." + resource},
                    @LockMode='Exclusive', @LockOwner='Session', @LockTimeout=10000;
                IF @result < 0 THROW 51001, 'Concurrent edit timeout', 1;
                """, context.HttpContext.RequestAborted);
            acquired = true;
            // SaveChanges owns its transaction, so upload cleanup runs only after a committed save.
            await next();
        }
        catch (SqlException ex) when (ex.Number is 51001 or 1205)
        {
            context.Result = new ObjectResult("Başka bir kayıt işlemi sürüyor. Lütfen tekrar deneyin.") { StatusCode=409 };
        }
        finally
        {
            try
            {
                if (acquired)
                    await db.Database.ExecuteSqlInterpolatedAsync($"EXEC sys.sp_releaseapplock @Resource={"Portfolio.Admin." + resource}, @LockOwner='Session';");
            }
            finally { await db.Database.CloseConnectionAsync(); }
        }
    }
}
