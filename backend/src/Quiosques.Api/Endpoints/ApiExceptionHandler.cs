using Microsoft.AspNetCore.Diagnostics;
using Quiosques.Application.Common;
using Quiosques.Domain;

namespace Quiosques.Api.Endpoints;

// Converte as exceções da aplicação em respostas HTTP com { "message": "..." }.
public class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // Corpo da requisição ilegível (JSON malformado ou campo com tipo errado).
        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.StatusCode = badRequest.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(new ErrorResponse("Requisição inválida"), cancellationToken);
            return true;
        }

        var status = exception switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            DomainException => StatusCodes.Status422UnprocessableEntity,
            _ => (int?)null,
        };

        if (status is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = status.Value;
        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse(exception.Message), cancellationToken);
        return true;
    }
}
