using System.Net;

namespace TrainScrapingApi.Models.Exceptions
{
    public class ForbiddenException : HttpException
    {
        public ForbiddenException(string message, int code)
            : this(message, code, null)
        {
        }

        public ForbiddenException(string message, int code, Exception? innerException)
            : base(HttpStatusCode.Forbidden, message, code, innerException)
        {
        }
    }
}
