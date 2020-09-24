# Maskinport-integrasjon-fa Function app - generisk
## Komme i gang
* Begynt med HttpTrigger template i Visual Studio Code
* Test virksomhetssertifikat er opprettet og lagt inn i test Key vault. Det skal også ligger en tilsvarende prod sertifikat i prod til senere.
* Hvis man skal jobbe med sertifikater trenger man tilgang til det - må kunne liste og gette sertifikater og secrets og tildeler tilganger.
## Henter Sertifikat fra Key vault
1. Gir function app tilganger til sertifikat
   1. Lag en Managed Identity slik som det er beskrevet her: https://daniel-krzyczkowski.github.io/Integrate-Key-Vault-Secrets-With-AzureFunctions/
   1. Vår app trenger Get permission på sertifikater og secrets
   1. Husker å klikke "save"
1. Henter sertifikat - har funnet ut til at det må hentes ut som secret for at private key er med. Følgende kode kan brukes
   1. Her må din keyvault navn og sertifikat navn legges inn
``` C#
private static async Task<X509Certificate2> GetX509Certificate2()
{
    SecretClient client = new SecretClient(vaultUri: new Uri ("https://<din keyvault navn>.vault.azure.net/"), 
        credential: new DefaultAzureCredential());
    KeyVaultSecret secret = await client.GetSecretAsync("<sertifikat navn>");
    X509Certificate2 certificate = new X509Certificate2(Convert.FromBase64String(secret.Value));
    return certificate;
 }
 ```
## Henter token fra Maskinporten
1. Lage kall mot MaskinID porten. Bruker https://github.com/ks-no/fiks-maskinporten-client-dotnet
   1. her må din Integrasjons identifikator legges inn - finnes hvor dere opprettet Integrasjonen i difi sin selvbetjeningsportal
``` C#
private static async Task<MaskinportenToken> GetMaskinportenToken(X509Certificate2 x509Certificate2)
{
    MaskinportenClientConfiguration maskinportenConfig = new MaskinportenClientConfiguration(
        audience: @"https://oidc-ver2.difi.no/idporten-oidc-provider/", // ID-porten audience path
        tokenEndpoint: @"https://oidc-ver2.difi.no/idporten-oidc-provider/token", // ID-porten token path
        issuer: @"<din Integrasjons identifikator>", // Integrasjonens identifikator fra difi Integrasjon i selvbetjeningsportal
        numberOfSecondsLeftBeforeExpire: 10, // The token will be refreshed 10 seconds before it expires
        certificate: x509Certificate2); // Virksomhetssertifikat as a X509Certificate2 (se f.eks. metoden over)
    MaskinportenClient maskinportenClient = new MaskinportenClient(maskinportenConfig);
    String scope = "ks:fiks"; // Scope for access token
    MaskinportenToken accessToken = await maskinportenClient.GetAccessToken(scope);
    return accessToken;
 }
 ```
## Nødvendige Packages
Følgende trenger vi å ha med i .csproj filen for at det skal fungerer:
``` XML
<PackageReference Include="Microsoft.NET.Sdk.Functions" Version="3.0.7" />
<PackageReference Include="KS.Fiks.Maskinporten.Client" Version="1.0.2" />
<PackageReference Include="Azure.Core" Version="1.5.0" />
<PackageReference Include="Azure.Security.KeyVault.Secrets" Version="4.1.0" />
<PackageReference Include="Azure.Identity" Version="1.3.0-beta.1" />
```
## Mer informasjon
* [Difi dokumentasjon på token ](https://difi.github.io/felleslosninger/oidc_protocol_token.html)
* [Microsoft informasjon om å kalle function app fra logic app](https://difi.github.io/felleslosninger/oidc_protocol_token.html)
* [KS sin fiks-maskinporten-client-dotnet](https://github.com/ks-no/fiks-maskinporten-client-dotnet) (kode og dokumentasjon)
* [Difi selvbetjeningsløsning](https://selvbetjening-samarbeid-ver2.difi.no/)
