using Microsoft.AspNetCore.Mvc;

namespace WarehouseApi.ErrorHandling;

public interface IExceptionToProblemDetailsMapper
{
    ProblemDetails Map(Exception exception, HttpContext context);
}
