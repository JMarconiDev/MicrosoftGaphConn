using Microsoft.AspNetCore.Mvc;

namespace MicrosoftGraphConn.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AzureAuthController : ControllerBase
    {
        private static readonly string CLIENT_ID =
            Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? "Falta AZURE_CLIENT_ID";

        private static readonly string TENANT_ID =
            Environment.GetEnvironmentVariable("AZURE_TENANT_ID") ?? "Falta AZURE_TENANT_ID";

        private static readonly string REDIRECT_URI =
            Environment.GetEnvironmentVariable("AZURE_REDIRECT_URI") ?? "Falta AZURE_REDIRECT_URI";

        private static readonly string CLIENT_SECRET =
            Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET") ?? "Falta AZURE_CLIENT_SECRET";

        private const string SCOPE = "User.Read openid profile offline_access";

        [HttpGet("authorize")]
        public IActionResult Authorize()
        {
            var url = $"https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/authorize" +
                      $"?response_type=code" +
                      $"&client_id={CLIENT_ID}" +
                      $"&redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}" +
                      $"&scope={Uri.EscapeDataString(SCOPE)}";

            return Redirect(url);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest("Parâmetro 'code' ausente.");
            }

            var tokenUrl = $"https://login.microsoftonline.com/{TENANT_ID}/oauth2/v2.0/token";

            var formData = new Dictionary<string, string>
            {
                { "client_id", CLIENT_ID },
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", REDIRECT_URI },
                { "scope", SCOPE },
                { "client_secret", CLIENT_SECRET }
            };

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(tokenUrl, new FormUrlEncodedContent(formData));
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, content);
                }

                return Content(content, "application/json");
            }
        }
    }
}
