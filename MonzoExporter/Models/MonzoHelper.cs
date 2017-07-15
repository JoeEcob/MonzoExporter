using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Mondo;
using Newtonsoft.Json;

namespace MonzoExporter.Models
{
    class MonzoHelper
    {
        private IConfiguration _config;
        private AccessToken _accessToken;

        private string OAuthPath => Path.Combine(Directory.GetCurrentDirectory(), _config["monzo_oauth_path"]);

        public MonzoHelper(IConfiguration config)
        {
            _config = config;
        }

        public async Task<AccessToken> AccessToken()
        {
            if (_accessToken == null)
            {
                try
                {
                    var json = File.ReadAllText(OAuthPath);
                    _accessToken = JsonConvert.DeserializeObject<AccessToken>(json);
                }
                catch (FileNotFoundException)
                {
                    _accessToken = await SetupNewToken();
                }
            }

            return _accessToken;
        }

        public PaginationOptions PaginationOptions
        {
            get
            {
                var yesterday = DateTime.Now.AddDays(-1);

                return new PaginationOptions
                {
                    SinceTime = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 0, 0, 0),
                    BeforeTime = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 23, 59, 59)
                };
            }
        }

        private async Task<AccessToken> SetupNewToken()
        {
            var authClient = new MondoAuthorizationClient(_config["monzo_client_id"], _config["monzo_client_secret"]);

            var loginPageUrl = authClient.GetAuthorizeUrl(null, _config["monzo_redirect_uri"]);

            Console.WriteLine("Visit the following URL to get the magic code:");
            Console.WriteLine(loginPageUrl);
            Console.WriteLine("Enter magic code:");

            var code = Console.ReadLine();

            var accessToken = await authClient.GetAccessTokenAsync(code, _config["monzo_redirect_uri"]);

            var file = File.CreateText(OAuthPath);
            await file.WriteLineAsync(JsonConvert.SerializeObject(accessToken));
            file.Dispose();

            Console.WriteLine($"Successfully created {OAuthPath}!");

            return accessToken;
        }
    }
}
