using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Mondo;
using MonzoExporter.Models;

namespace MonzoExporter.Helpers
{
    class GoogleHelper
    {
        private readonly AppSettings _config;
        private SheetsService _sheetsService;

        private string OAuthPath => _config.OAuthPath;

        public GoogleHelper(AppSettings config)
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
                        ClientId = _config.GoogleClientId,
                        ClientSecret = _config.GoogleClientSecret
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
                var created = item.Created.ToString("G", new CultureInfo("en-GB"));
                var payee = item.Merchant?.Name ?? item.Description;
                var amount = Convert.ToDecimal(item.Amount) / 100; // Convert from pence to pounds
                var balance = Convert.ToDecimal(item.AccountBalance) / 100;

                var cells = new[] { created, payee, item.Category, item.Notes, amount.ToString(), balance.ToString() };
                var row = new List<object>(cells);

                values.Add(row);
                Console.WriteLine($"Added 1 row: {string.Join(" - ", cells)}");
            }

            return values;
        }

        public async Task<AppendValuesResponse> Append(IList<IList<object>> values)
        {
            var request = SheetsService.Spreadsheets.Values.Append(new ValueRange
            {
                MajorDimension = "ROWS",
                Range = _config.GoogleSpreadsheetRange,
                Values = values
            },
            _config.GoogleSpreadsheetId,
            _config.GoogleSpreadsheetRange);

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            
            return await request.ExecuteAsync();
        }
    }
}
