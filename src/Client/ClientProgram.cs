namespace Client
{
    using Addresses;
    using IdentityModel.Client;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    class ClientProgram
    {
        static async Task Main(string[] args)
        {
            Console.Title = "Client";

            await Console.Out.WriteAsync("Press <return> to call");
            await Console.In.ReadLineAsync();

            
            // var client = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_message, _cert, _chain, _errors) => true });

            var client = new HttpClient();
            var disco = await client.GetDiscoveryDocumentAsync(Address.STS);
            if (disco.IsError)
            {
                await Console.Error.WriteLineAsync(disco.Error);
                return;
            }

            var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = "client",
                ClientSecret = "secret",
                Scope = "api1"
            });

            if (tokenResponse.IsError)
            {
                await Console.Error.WriteLineAsync(tokenResponse.Error);
                return;
            }

            await Console.Out.WriteLineAsync(tokenResponse.Json.ToString());

            var apiClient = new HttpClient(new HttpClientHandler { ServerCertificateCustomValidationCallback = (_message, _cert, _chain, _errors) => true });
            apiClient.SetBearerToken(tokenResponse.AccessToken);

            var response = await apiClient.GetAsync(Address.Service);
            if (!response.IsSuccessStatusCode)
            {
                await Console.Error.WriteLineAsync(response.StatusCode.ToString());
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                await Console.Out.WriteLineAsync(JArray.Parse(content).ToString());
            }
        }
    }
}