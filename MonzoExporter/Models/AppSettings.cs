using System;
using System.Collections.Generic;
using System.Text;

namespace MonzoExporter.Models
{
    class AppSettings
    {
        public string OAuthPath { get; set; }

        public string GoogleClientId { get; set; }
        public string GoogleClientSecret { get; set; }
        public string GoogleSpreadsheetId { get; set; }
        public string GoogleSpreadsheetRange { get; set; }

        public string MonzoClientId { get; set; }
        public string MonzoClientSecret { get; set; }
        public string MonzoRedirectUri { get; set; }
    }
}
