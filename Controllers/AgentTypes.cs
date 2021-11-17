using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IdentityCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

public class AgentTypeModel
{
    public int Code { get; set; }
    public string Name { get; set; }
}

[Route("[controller]")]
public class AgentTypesController : Controller
{
    [HttpGet]
    public async Task<JsonResult> OnGetAsync()
    {
        var types = new List<AgentTypeModel>();

        using (var httpClientHandler = new HttpClientHandler())
        {
            httpClientHandler
                .ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            using (var client = new HttpClient(httpClientHandler))
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                try
                {
                    var agentTypeSource = Startup.Configuration.GetConnectionString("AgentTypesSourceHostUrl");
                    var response = await client.GetAsync(agentTypeSource);
                    var responseContent = await response.Content.ReadAsStringAsync();
                    types = JsonConvert.DeserializeObject<List<AgentTypeModel>>(responseContent);
                }
                catch
                {
                    types.Clear();
                    types.Add(new AgentTypeModel() { Code = 1, Name = "Man" });
                    types.Add(new AgentTypeModel() { Code = 2, Name = "Men" });
                    types.Add(new AgentTypeModel() { Code = 3, Name = "Machine" });
                }
            }
        }

        return new JsonResult(types);
    }
}