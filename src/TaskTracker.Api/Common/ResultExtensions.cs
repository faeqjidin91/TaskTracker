using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskTracker.Application.Common;

namespace TaskTracker.Api.Common;

// Translates Application results to HTTP in one place, which keeps controllers thin.
public static class ResultExtensions
{
    public static ActionResult<T> ToOk<T>(this ControllerBase controller, Result<T> result)
    {
        if (result.IsSuccess)
        {
            return controller.Ok(result.Value);
        }

        return controller.ToFailure(result);
    }

    public static ActionResult<T> ToCreated<T>(
        this ControllerBase controller,
        Result<T> result,
        string actionName,
        string controllerName,
        Func<T, object> routeValues)
    {
        if (result.IsSuccess)
        {
            return controller.CreatedAtAction(actionName, controllerName, routeValues(result.Value), result.Value);
        }

        return controller.ToFailure(result);
    }

    public static ActionResult ToNoContent(this ControllerBase controller, Result<Unit> result) =>
        result.IsSuccess ? controller.NoContent() : controller.ToFailure(result);

    private static ActionResult ToFailure<T>(this ControllerBase controller, Result<T> result)
    {
        switch (result.Status)
        {
            case ResultStatus.NotFound:
                return controller.Problem(statusCode: StatusCodes.Status404NotFound, title: "Resource not found.");

            case ResultStatus.Unauthorized:
                return controller.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");

            case ResultStatus.Conflict:
                return controller.Problem(statusCode: StatusCodes.Status409Conflict, title: result.Message);

            case ResultStatus.ValidationFailed:
                var modelState = new ModelStateDictionary();
                foreach (var (field, messages) in result.Errors)
                {
                    foreach (var message in messages)
                    {
                        modelState.AddModelError(field, message);
                    }
                }

                return controller.ValidationProblem(modelState);

            default:
                throw new InvalidOperationException($"Unexpected result status {result.Status}.");
        }
    }
}
