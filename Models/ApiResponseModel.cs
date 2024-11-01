using System.Net;

namespace Mathilda.Models
{
    public class ApiResponseModel<T>
    {
        public string Message { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public bool IsSuccess { get; set; }
        public T? Result { get; set; }
    }
}
