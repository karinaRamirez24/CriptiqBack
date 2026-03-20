using Cryptiq.Common;
using Microsoft.AspNetCore.Mvc;

namespace Cryptiq.Controllers
{
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected IActionResult CreateResponse<T>(string message, ResponseStatus status, T data = default)
        {
            var response = new ApiResponse<T>(message, status, data);

            return status switch
            {
                ResponseStatus.Success => Ok(response),
                ResponseStatus.Created => StatusCode(201, response),
                ResponseStatus.NoContent => NoContent(),

                ResponseStatus.BadRequest => BadRequest(response),
                ResponseStatus.Unauthorized => Unauthorized(response),
                ResponseStatus.Forbidden => StatusCode(403, response),
                ResponseStatus.NotFound => NotFound(response),
                ResponseStatus.Conflict => Conflict(response),

                ResponseStatus.ValidationError => UnprocessableEntity(response),

                _ => StatusCode(500, response)
            };
        }

    

        protected IActionResult Success<T>(T data)
            => CreateResponse(Messages.General.Success, ResponseStatus.Success, data);

        protected IActionResult Error(string message)
            => CreateResponse<object>(message, ResponseStatus.Error);

        protected IActionResult UnauthorizedResponse()
            => CreateResponse<object>(Messages.Auth.Unauthorized, ResponseStatus.Unauthorized);

        protected IActionResult ValidationError(string message)
            => CreateResponse<object>(message, ResponseStatus.ValidationError);

        protected IActionResult NotFoundResponse(string message)
            => CreateResponse<object>(message, ResponseStatus.NotFound);
    }
}