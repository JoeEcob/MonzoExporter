using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Mondo;
using Newtonsoft.Json;
using System.Threading;

namespace MonzoExporter.Models
{
    class MonzoHelper
    {
        private IConfiguration _config;
        private MondoAuthorizationClient _client;
        private AccessToken _accessToken;

        private string OAuthPath => Path.Combine(_config["oauth_path"], "monzo-oauth.json");

        public MonzoHelper(IConfiguration config)
        {
            _config = config;
            _client = new MondoAuthorizationClient(config["monzo_client_id"], config["monzo_client_secret"]);
        }

        public AccessToken AccessToken
        {
            get
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
                        _accessToken = SetupNewToken().Result;
                    }
                }

                return _accessToken;
            }
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

        public async Task<AccessToken> RefreshToken()
        {
            var token = await _client.RefreshAccessTokenAsync(AccessToken.RefreshToken, CancellationToken.None);

            _accessToken = token;
            await StoreToken(token);

            return token;
        }

        private async Task<AccessToken> SetupNewToken()
        {
            var loginPageUrl = _client.GetAuthorizeUrl(null, _config["monzo_redirect_uri"]);

            Console.WriteLine("Visit the following URL to get the magic code:");
            Console.WriteLine(loginPageUrl);
            Console.WriteLine("Enter magic code:");

            var code = Console.ReadLine();

            var accessToken = await _client.GetAccessTokenAsync(code, _config["monzo_redirect_uri"]);

            await StoreToken(accessToken);

            Console.WriteLine($"Successfully created {OAuthPath}!");

            return accessToken;
        }

        private async Task StoreToken(AccessToken token)
        {
            var file = File.CreateText(OAuthPath);
            await file.WriteLineAsync(JsonConvert.SerializeObject(token));
            file.Dispose();
        }
    }
}
