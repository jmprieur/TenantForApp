using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WebApp_OpenIDConnect_DotNet.Services.Arm;

namespace TenantForApp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("TenantForApp <appId>");
                return;
            }

            // Get the application id to search
            string appId = args[0];

            FindTenant(appId).GetAwaiter().GetResult();
        }

        private static async Task FindTenant(string appId)
        {
            string clientID = "4561113a-c4c7-4dae-b61d-c5fb917a66eb";

            IPublicClientApplication app = PublicClientApplicationBuilder.Create(clientID)
                .WithRedirectUri("http://localhost")
                .Build();

            string[] scopes = new string[] { ArmApiOperationService.ArmResource + "/.default" };
            AuthenticationResult result = await app.AcquireTokenInteractive(scopes)
                .ExecuteAsync();

            // Find tenants id for signed-in user
            HttpClient client = new HttpClient();
            ArmApiOperationService armApiOperationService = new ArmApiOperationService(client);
            var tenantIDs = await armApiOperationService.EnumerateTenantsIdsAccessibleByUser(result.AccessToken);

            string[] scopes2 = new string[] { "Directory.Read.All" };

            foreach (string tenantIUD in tenantIDs)
            {
                AuthenticationResult result2;
                result2 = await app.AcquireTokenSilent(scopes2, result.Account)
                    .WithAuthority(AzureCloudInstance.AzurePublic, new Guid(tenantIUD))
                    .ExecuteAsync();

                HttpClient client2 = new HttpClient();
                client2.DefaultRequestHeaders.Add("Authorization", $"Bearer {result2.AccessToken}");

                var httpResult = await client2.GetAsync($"https://graph.microsoft.com/beta/applications?$filter=appId eq '{appId}'");
                string json = await httpResult.Content.ReadAsStringAsync();
                Apps applications = (dynamic)JsonSerializer.Deserialize<Apps>(json);

                if (applications.value.Any())
                {
                    App theApp = applications.value.FirstOrDefault();
                    Console.WriteLine($"{theApp.displayName} in {theApp.publisherDomain}");
                }
            }


            // Find if app is in tenant
        }
    }

    public class Apps
    {
        public List<App> value { get; set; }
    }
    
    public class App
    {
        public string displayName { get; set; }

        public string publisherDomain { get; set; }
    }
}
