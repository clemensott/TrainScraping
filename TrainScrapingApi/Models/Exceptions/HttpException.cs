using System.Net;

namespace TrainScrapingApi.Models.Exceptions
{
    public class HttpException : Exception
    {
        public HttpStatusCode Status { get; }

        public int Code { get; }

        public HttpException(HttpStatusCode status, string message, int code) : base(message)
        {
            Status = status;
            Code = code;
        }

        public HttpException(HttpStatusCode status, string message, int code, Exception? innerException) : base(message, innerException)
        {
            Status = status;
            Code = code;
        }
    }
}
