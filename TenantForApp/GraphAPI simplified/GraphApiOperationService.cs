
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebApp_OpenIDConnect_DotNet.Services.GraphOperations
{
    public class GraphApiOperationService : IGraphApiOperations
    {
        private readonly HttpClient httpClient;

        public GraphApiOperationService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<IDictionary<string, string>> EnumerateTenantsIdAndNameAccessibleByUser(IEnumerable<string> tenantIds, Func<string, Task<string>> getTokenForTenant)
        {
            Dictionary<string, string> tenantInfo = new Dictionary<string, string>();
            foreach (string tenantId in tenantIds)
            {
                string displayName;
                try
                {
                    string accessToken = await getTokenForTenant(tenantId);
                    httpClient.DefaultRequestHeaders.Remove("Authorization");
                    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                    var httpResult = await httpClient.GetAsync(GraphTenantInfoUrl);
                    var json = await httpResult.Content.ReadAsStringAsync();
                    OrganizationResult organizationResult = JsonSerializer.Deserialize<OrganizationResult>(json);
                    displayName = organizationResult.value.First().displayName;
                }
                catch
                {
                    displayName = "you need to sign-in (or have the admin consent for the app) in that tenant";
                }

                tenantInfo.Add(tenantId, displayName);
            }
            return tenantInfo;
        }

        // Use the graph to get information (name) for a tenant 
        // See https://docs.microsoft.com/en-us/graph/api/organization-get?view=graph-rest-beta
        protected string GraphTenantInfoUrl { get; } = "https://graph.microsoft.com/beta/organization";
    }

    /// <summary>
    /// Result for a call to graph/organizations.
    /// </summary>
    class OrganizationResult
    {
        public Organization[] value { get; set; }
    }

    /// <summary>
    /// We are only interested in the organization display name
    /// </summary>
    class Organization
    {
        public string displayName { get; set; }
    }
}