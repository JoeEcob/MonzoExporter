using System;

namespace MonzoExporter.Models
{
    internal class AppSettings
    {
        private static DateTime _yesterday = DateTime.Now.AddDays(-1);

        public string OAuthPath { get; set; }

        public string GoogleClientId { get; set; }
        public string GoogleClientSecret { get; set; }
        public string GoogleSpreadsheetId { get; set; }
        public string GoogleSpreadsheetRange { get; set; }

        public string MonzoClientId { get; set; }
        public string MonzoClientSecret { get; set; }
        public string MonzoRedirectUri { get; set; }

        public string CsvExportPath { get; set; }

        public DateTime? SinceTime { get; set; }
            = new DateTime(_yesterday.Year, _yesterday.Month, _yesterday.Day, 0, 0, 0);

        public string EmailApiKey { get; set; }
        public string EmailDomain { get; set; }
        public string EmailFromAddress { get; set; }
        public string EmailToAddress { get; set; }
    }
}
