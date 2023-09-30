using TrainScrapingApi.Models.Exceptions;
using TrainScrapingApi.Services.Database;

namespace TrainScrapingApi.Middlewares
{
    public class BasicAuthorizationMiddleware
    {
        private readonly IUsersRepo usersRepo;
        private readonly RequestDelegate next;

        public BasicAuthorizationMiddleware(RequestDelegate next, IUsersRepo usersRepo)
        {
            this.usersRepo = usersRepo;
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                throw new UnauthorizedException("No authorization", 1001);
            }

            string auth = context.Request.Headers["Authorization"];
            if (!auth.ToLower().StartsWith("basic "))
            {
                throw new UnauthorizedException("No basic authorization", 1002);
            }

            string token = auth[6..];
            if (!await usersRepo.AuthAsync(token))
            {
                throw new ForbiddenException("Forbidden", 1003);
            }
            await next(context);
        }
    }
}
