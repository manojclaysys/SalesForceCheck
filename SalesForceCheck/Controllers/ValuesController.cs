using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace SalesForceCheck.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string loginUrl;
        private readonly string client_id;
        private readonly string username;
        private readonly string password;
        private readonly string apiVersion;
        private readonly string client_secret;

        public ValuesController(IConfiguration configuration)
        {
            _configuration = configuration;
            loginUrl = _configuration["SalesforceSettings:LoginUrl"];
            client_id = _configuration["SalesforceSettings:client_id"];
            username = _configuration["SalesforceSettings:Username"];
            password = _configuration["SalesforceSettings:Password"];
            apiVersion = _configuration["SalesforceSettings:ApiVersion"];
            client_secret = _configuration["SalesforceSettings:client_secret"];
        }

        private (string accessToken, string instanceUrl) Authenticate()
        {
            RestClient client = new RestClient(loginUrl);
            RestRequest request = new RestRequest("/services/oauth2/token", Method.Post);
            request.AddParameter("grant_type", "password");
            request.AddParameter("client_id", client_id);
            request.AddParameter("client_secret", client_secret);
            request.AddParameter("username", username);
            request.AddParameter("password", password);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
                string accessToken = data["access_token"];
                string instanceUrl = data["instance_url"];
                return (accessToken, instanceUrl);
            }
            else
            {
                Console.WriteLine($"Error: {response.StatusCode}");
                Console.WriteLine($"Response Content: {response.Content}");
                return (null, null);
            }
        }

        private RestClient CreateApiClient(string instanceUrl, string accessToken)
        {
            RestClient apiClient = new RestClient(instanceUrl);
            apiClient.AddDefaultHeader("Authorization", $"Bearer {accessToken}");
            return apiClient;
        }

        [HttpPost]
        public IActionResult PostOpportunity(OpportunityModel opportunity)
        {
            var authInfo = Authenticate();
            if (authInfo.accessToken != null && authInfo.instanceUrl != null)
            {
                var (accessToken, instanceUrl) = authInfo;
                RestClient apiClient = CreateApiClient(instanceUrl, accessToken);

                RestRequest apiRequest = new RestRequest($"/services/data/v{apiVersion}/sobjects/Opportunity", Method.Post);
                apiRequest.AddHeader("Content-Type", "application/json");
                apiRequest.AddJsonBody(new
                {
                    Name = opportunity.Name,
                    StageName = opportunity.StageName,
                    CloseDate = opportunity.CloseDate,
                    Amount = opportunity.Amount
                });
                var apiResponse = apiClient.Execute(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.Created)
                {
                    return Ok("Opportunity created successfully.");
                }
                else
                {
                    return BadRequest($"Error: {apiResponse.Content}");
                }
            }
            else
            {
                return BadRequest("Authentication failed.");
            }
        }

        [HttpPut("{id}")]
        public IActionResult PutOpportunity(string id, OpportunityModel opportunity)
        {
            var authInfo = Authenticate();
            if (authInfo.accessToken != null && authInfo.instanceUrl != null)
            {
                var (accessToken, instanceUrl) = authInfo;
                RestClient apiClient = CreateApiClient(instanceUrl, accessToken);

                RestRequest apiRequest = new RestRequest($"/services/data/v{apiVersion}/sobjects/Opportunity/{id}", Method.Patch);
                apiRequest.AddHeader("Content-Type", "application/json");
                apiRequest.AddJsonBody(new
                {
                    StageName = opportunity.StageName,
                    CloseDate = opportunity.CloseDate,
                    Amount = opportunity.Amount
                });
                var apiResponse = apiClient.Execute(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return Ok("Opportunity updated successfully.");
                }
                else
                {
                    return BadRequest($"Error: {apiResponse.Content}");
                }
            }
            else
            {
                return BadRequest("Authentication failed.");
            }
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteOpportunity(string id)
        {
            var authInfo = Authenticate();
            if (authInfo.accessToken != null && authInfo.instanceUrl != null)
            {
                var (accessToken, instanceUrl) = authInfo;
                RestClient apiClient = CreateApiClient(instanceUrl, accessToken);

                RestRequest apiRequest = new RestRequest($"/services/data/v{apiVersion}/sobjects/Opportunity/{id}", Method.Delete);
                var apiResponse = apiClient.Execute(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.NoContent)
                {
                    return Ok("Opportunity deleted successfully.");
                }
                else
                {
                    return BadRequest($"Error: {apiResponse.Content}");
                }
            }
            else
            {
                return BadRequest("Authentication failed.");
            }
        }

        [HttpGet("{id}")]
        public IActionResult GetOpportunity(string id)
        {
            var authInfo = Authenticate();
            if (authInfo.accessToken != null && authInfo.instanceUrl != null)
            {
                var (accessToken, instanceUrl) = authInfo;
                RestClient apiClient = CreateApiClient(instanceUrl, accessToken);

                RestRequest apiRequest = new RestRequest($"/services/data/v{apiVersion}/sobjects/Opportunity/{id}", Method.Get);
                var apiResponse = apiClient.Execute(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.OK)
                {
                    var opportunityData = JsonConvert.DeserializeObject<Dictionary<string, object>>(apiResponse.Content);
                    return Ok(opportunityData);
                }
                else
                {
                    return BadRequest($"Error: {apiResponse.Content}");
                }
            }
            else
            {
                return BadRequest("Authentication failed.");
            }
        }

        [HttpGet]
        public IActionResult GetAllOpportunities()
        {
            var authInfo = Authenticate();
            if (authInfo.accessToken != null && authInfo.instanceUrl != null)
            {
                var (accessToken, instanceUrl) = authInfo;
                RestClient apiClient = CreateApiClient(instanceUrl, accessToken);

                RestRequest apiRequest = new RestRequest($"/services/data/v{apiVersion}/query/", Method.Get);
                apiRequest.AddParameter("q", "SELECT Id, Name, StageName, CloseDate, Amount FROM Opportunity");
                var apiResponse = apiClient.Execute(apiRequest);

                if (apiResponse.StatusCode == HttpStatusCode.OK)
                {
                    var jsonResponse = JsonConvert.DeserializeObject<JObject>(apiResponse.Content);
                    var records = jsonResponse["records"].ToObject<List<JObject>>();
                    var recordList = records.Select(r => r.ToObject<Dictionary<string, object>>()).ToList();
                    return Ok(recordList);
                }
                else
                {
                    return BadRequest($"Error: {apiResponse.Content}");
                }
            }
            else
            {
                return BadRequest("Authentication failed.");
            }
        }
    }

    public class OpportunityModel
    {
        public string Name { get; set; }
        public string StageName { get; set; }
        public string Amount { get; set; }
        public DateTime CloseDate { get; set; }
    }
}
      
