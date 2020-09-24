using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ks.Fiks.Maskinporten.Client;
using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
 
namespace Baerum.Kommune.Functions
{
    public static class HentMaskinportToken
    {
        [FunctionName("HentMaskinportToken")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            X509Certificate2 x509Certificate2 = await GetX509Certificate2();
            log.LogInformation("Hentet certificate med privatekey: " + x509Certificate2.GetRSAPrivateKey().ToString()); 

            MaskinportenToken token = await GetMaskinportenToken(x509Certificate2);
            log.LogInformation("Hentet token: " + token.ToString());

            log.LogInformation("Returner token - function ferdig");
            return new OkObjectResult(token);
        }

         private static async Task<X509Certificate2> GetX509Certificate2()
         { 
            SecretClient client = new SecretClient(vaultUri: new Uri ("https://<key vault navn>.vault.azure.net/"), 
                credential: new DefaultAzureCredential());
            KeyVaultSecret secret = await client.GetSecretAsync("<sertifikate navn>>");
            X509Certificate2 certificate = new X509Certificate2(Convert.FromBase64String(secret.Value));          
            return certificate;
        } 

        private static async Task<MaskinportenToken> GetMaskinportenToken(X509Certificate2 x509Certificate2) 
        {
            MaskinportenClientConfiguration maskinportenConfig = new MaskinportenClientConfiguration(
                audience: @"https://oidc-ver2.difi.no/idporten-oidc-provider/", // ID-porten audience path
                tokenEndpoint: @"https://oidc-ver2.difi.no/idporten-oidc-provider/token", // ID-porten token path
                issuer: @"<Integrasjonens identifikator>",  // Integrasjonens identifikator fra difi Integrasjon i selvbetjeningsportal
                numberOfSecondsLeftBeforeExpire: 10, // The token will be refreshed 10 seconds before it expires
                certificate: x509Certificate2);  // Virksomhetssertifikat as a X509Certificate2

            MaskinportenClient maskinportenClient = new MaskinportenClient(maskinportenConfig);
            String scope = "ks:fiks"; // Scope for access token
            MaskinportenToken accessToken = await maskinportenClient.GetAccessToken(scope);
            return accessToken;
        }
    }
}
