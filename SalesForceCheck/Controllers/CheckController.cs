using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace SalesForceCheck.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string loginUrl;
        private readonly string client_id;
        private readonly string username;
        private readonly string password;
        private readonly string apiVersion;
        private readonly string client_secret;

        public CheckController(IConfiguration configuration)
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

        [HttpPost]
        public IActionResult PostOpportunity(OpportunityModel opportunity)
        {
            var authInfo = Authenticate();
            if (authInfo.accessToken != null && authInfo.instanceUrl != null)
            {
                var (accessToken, instanceUrl) = authInfo;
                RestClient apiClient = new RestClient(instanceUrl);
                RestRequest apiRequest = new RestRequest($"/services/data/v{apiVersion}/sobjects/Opportunity", Method.Post);
                apiRequest.AddHeader("Authorization", $"Bearer {accessToken}");
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
                RestClient apiClient = new RestClient(instanceUrl);
                RestRequest apiRequest = new RestRequest($"/services/data/v{apiVersion}/sobjects/Opportunity/{id}", Method.Patch);
                apiRequest.AddHeader("Authorization", $"Bearer {accessToken}");
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
    }
}
