using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Mondo;
using System;

namespace MonzoExporter.Models
{
    class GoogleHelper
    {
        private IConfiguration _config;
        private SheetsService _sheetsService;

        private string OAuthPath => Path.Combine(Directory.GetCurrentDirectory(), _config["google_oauth_path"]);

        public GoogleHelper(IConfiguration config)
        {
            _config = config;
        }

        public SheetsService SheetsService
        {
            get
            {
                if (_sheetsService == null)
                {
                    var secrets = new ClientSecrets
                    {
                        ClientId = _config["google_client_id"],
                        ClientSecret = _config["google_client_secret"]
                    };

                    var credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                        secrets,
                        new[] { SheetsService.Scope.Spreadsheets },
                        "monzo-exporter",
                        CancellationToken.None,
                        new FileDataStore(OAuthPath, true)
                    ).Result;

                    _sheetsService = new SheetsService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = "MonzoExporter",
                    });
                }

                return _sheetsService;
            }
        }

        public IList<IList<object>> BuildList(IList<Transaction> transactions)
        {
            var values = new List<IList<object>>();

            foreach (var item in transactions)
            {
                var payee = item.Merchant?.Name == null ? item.Description : item.Merchant.Name;
                var amount = Convert.ToDecimal(item.Amount) / 100; // Convert from pence to pounds
                var balance = Convert.ToDecimal(item.AccountBalance) / 100;

                var cells = new string[] { item.Created.ToString(), payee, item.Category, item.Notes, amount.ToString(), balance.ToString() };
                var row = new List<object>(cells);

                values.Add(row);
                Console.WriteLine($"Added 1 row: {String.Join(" - ", cells)}");
            }

            return values;
        }

        public async Task<AppendValuesResponse> Append(IList<IList<object>> values)
        {
            var request = SheetsService.Spreadsheets.Values.Append(new ValueRange
            {
                MajorDimension = "ROWS",
                Range = _config["google_spreadsheet_range"],
                Values = values
            },
            _config["google_spreadsheet_id"],
            _config["google_spreadsheet_range"]);

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            
            return await request.ExecuteAsync();
        }
    }
}
