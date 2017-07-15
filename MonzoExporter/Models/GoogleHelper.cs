using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace MonzoExporter.Models
{
    class GoogleHelper
    {
        private IConfiguration _config;
        private SheetsService _sheetsService;

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
                        "user",
                        CancellationToken.None
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

            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            
            return await request.ExecuteAsync();
        }
    }
}
