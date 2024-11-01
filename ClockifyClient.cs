using System.Text.Json;
using System.Text;
using Mathilda.Models;

namespace Mathilda
{
    public interface IClockifyClient
    {
        public Task<ApiResponseModel<Tout>> Post<T, Tout>(T payload, string url);
    }
    public class ClockifyClient : IClockifyClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        public ClockifyClient(HttpClient client, ILogger<ClockifyClient> logger) 
        {
            _httpClient = client;
            _logger = logger;
        }
        public async Task<ApiResponseModel<Tout>> Post<T, Tout>(T payload, string url)
        {
            try
            {
                HttpRequestMessage request = new(HttpMethod.Post, url)
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                using HttpResponseMessage response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                string content = await response.Content.ReadAsStringAsync();

                var deserializedContent = 
                    JsonSerializer.Deserialize<Tout>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return new ApiResponseModel<Tout>() 
                { 
                    IsSuccess = true, 
                    StatusCode = response.StatusCode,
                    Message = "Ok" ,
                    Result = deserializedContent
                };
            }
            catch (Exception e)
            {
                _logger.LogError($"An error occured while executing {nameof(Post)}, Error:{e.Message}, Resource:{url}");
                return new ApiResponseModel<Tout>() 
                { 
                    IsSuccess = false, 
                    StatusCode = System.Net.HttpStatusCode.InternalServerError, 
                    Message = "Whooooops" 
                };
            }
        }
    }
}
