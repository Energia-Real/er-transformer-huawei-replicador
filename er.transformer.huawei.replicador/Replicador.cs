using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace er.transformer.huawei.replicador;

public class Replicador
{
    private readonly ILogger _logger;

    public Replicador(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<Replicador>();
    }

    [Function("Replicador")]
    public async Task RunAsync([TimerTrigger("0 */10 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        await Requests();

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
    }

    private static async Task<UserInfo> LoginAsync(string url, object requestBody)
    {
        using (var client = new HttpClient())
        {
            var json = JsonConvert.SerializeObject(requestBody);
            var data = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, data);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var userinfo = JsonConvert.DeserializeObject<UserInfo>(responseString);
                return userinfo;
            }
            else
            {
                throw new Exception("Error: " + response.StatusCode);
            }
        }
    }
    private static async Task<string> PostAsync(string url, string token)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PostAsync(url, null);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                return responseString;
            }
            else
            {
                throw new Exception("Error: " + response.StatusCode);
            }
        }
    }

    private async Task Requests() {

        var urlAuth = "https://er-backoffice-auth-ms-mx.azurewebsites.net/api/v1/login";
        var requestBody = new
        {
            email = "admin.portal@energiareal.mx",
            password = "P455w0rd.1"
        };

        var userinfo = await LoginAsync(urlAuth, requestBody);
        Console.WriteLine($"Token: {userinfo.Token}");
        Console.WriteLine($"AccessTo: {userinfo.AccessTo}");

        var urlTransformerHuawei = "https://er-transformer-proxy-int.azurewebsites.net/api/v1/integrators/proxy/replicateToMongoDb";

        var response = await PostAsync(urlTransformerHuawei, userinfo.Token);
        Console.WriteLine($"Replico: {response}");

    }
}
