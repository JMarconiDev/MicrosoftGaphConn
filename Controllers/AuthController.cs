using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MicrosoftGraphConn.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AzureAuthController : ControllerBase
    {
        private static readonly string CLIENT_ID = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? throw new ArgumentNullException("AZURE_CLIENT_ID");
        private static readonly string TENANT_ID = Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? throw new ArgumentNullException("AZURE_TENANT_ID");
        private static readonly string CLIENT_SECRET = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? throw new ArgumentNullException("AZURE_CLIENT_SECRET");

        private const string GRAPH_SCOPE = "https://graph.microsoft.com/.default";
        private const string GRAPH_API_URL = "https://graph.microsoft.com/v1.0/me";

        private static IConfidentialClientApplication? _clientApp;

        public AzureAuthController()
        {
            _clientApp = ConfidentialClientApplicationBuilder.Create(CLIENT_ID)
                .WithClientSecret(CLIENT_SECRET)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{TENANT_ID}"))
                .Build();
        }

        [HttpGet("get-token")]
        public async Task<IActionResult> GetAccessToken()
        {
            try
            {
                var result = await _clientApp.AcquireTokenForClient(new[] { GRAPH_SCOPE }).ExecuteAsync();
                return Ok(new { accessToken = result.AccessToken });
            }
            catch (MsalException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("graph-me")]
        public async Task<IActionResult> GetGraphMe()
        {
            try
            {
                var authResult = await _clientApp.AcquireTokenForClient(new[] { GRAPH_SCOPE }).ExecuteAsync();
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

                var response = await httpClient.GetAsync(GRAPH_API_URL);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, content);
                }

                var json = JsonSerializer.Deserialize<object>(content);
                return Ok(json);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
