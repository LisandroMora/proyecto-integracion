using Microsoft.AspNetCore.Diagnostics;
using Nomina.Application.Exceptions;

namespace Nomina.Api.Middleware;

public class DomainExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DomainValidationException dve) return false;

        httpContext.Response.StatusCode = dve.Status;
        await httpContext.Response.WriteAsJsonAsync(new { message = dve.Message }, cancellationToken);
        return true;
    }
}
