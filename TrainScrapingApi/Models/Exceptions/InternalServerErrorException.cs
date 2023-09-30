using System.Net;

namespace TrainScrapingApi.Models.Exceptions
{
    public class InternalServerErrorException : HttpException
    {
        public InternalServerErrorException(int code)
            : this(code, null)
        {
        }

        public InternalServerErrorException(int code, Exception? innerException)
            : this("Internal Server Error", code, innerException)
        {
        }

        public InternalServerErrorException(string message, int code, Exception? innerException)
            : base(HttpStatusCode.InternalServerError, message, code, innerException)
        {
        }
    }
}
