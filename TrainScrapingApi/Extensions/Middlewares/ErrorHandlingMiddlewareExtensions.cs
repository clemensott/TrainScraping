using TrainScrapingApi.Middlewares;

namespace TrainScrapingApi.Extensions.Middlewares
{
    static class ErrorHandlingMiddlewareExtensions
    {
        public static void UseGlobalErrorHandler(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                return;
            }

            app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
