using System;
using System.IO;
using System.Threading.Tasks;

using Monzo;
using Newtonsoft.Json;
using System.Threading;
using MonzoExporter.Models;

namespace MonzoExporter.Helpers
{
    internal class MonzoHelper
    {
        private readonly AppSettings _config;
        private readonly MonzoAuthorizationClient _client;
        private AccessToken _accessToken;

        private string OAuthPath => Path.Combine(_config.OAuthPath, "monzo-oauth.json");

        public MonzoHelper(AppSettings config)
        {
            _config = config;
            _client = new MonzoAuthorizationClient(_config.MonzoClientId, _config.MonzoClientSecret);
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
            => new PaginationOptions
            {
                BeforeTime = _config.Before,
                SinceTime = _config.Since
            };

        public async Task<AccessToken> RefreshToken()
        {
            var token = await _client.RefreshAccessTokenAsync(AccessToken.RefreshToken, CancellationToken.None);

            _accessToken = token;
            await StoreToken(token);

            return token;
        }

        private async Task<AccessToken> SetupNewToken()
        {
            var loginPageUrl = _client.GetAuthorizeUrl(null, _config.MonzoRedirectUri);

            Console.WriteLine("Visit the following URL to get the magic code:");
            Console.WriteLine(loginPageUrl);
            Console.WriteLine("Enter magic code:");

            var code = Console.ReadLine();

            var accessToken = await _client.GetAccessTokenAsync(code, _config.MonzoRedirectUri);

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
