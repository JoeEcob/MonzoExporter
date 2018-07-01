using System;
using static System.Console;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Configuration;

using Monzo;
using MonzoExporter.Helpers;
using System.Threading.Tasks;
using MonzoExporter.Models;
using RestSharp;
using RestSharp.Authenticators;

namespace MonzoExporter
{
    class Program
    {
        private const string ConfigFile = "appsettings.json";

        static void Main(string[] args)
        {
            var config = GetConfig();

            var type = args.Length > 0 ? args[0] : "";
            var sinceTime = args.Length > 1 ? args[1] : null;

            if (sinceTime != null && DateTime.TryParse(sinceTime, out var dateTime))
            {
                WriteLine($"Using custom since time: {dateTime:s}");
                config.SinceTime = dateTime;
            }

            var transactions = GetTransactions(config).Result;

            if (transactions.Count == 0)
            {
                WriteLine("No transactions found, exiting.");
                return;
            }

            WriteLine($"Found {transactions.Count} transactions to add.");

            switch (type.ToLower())
            {
                case "google":
                    ProcessGoogle(config, transactions);
                    break;
                case "csv":
                    ProcessCsv(config, transactions);
                    break;
                case "csv-to-email":
                    ProcessCsvToEmail(config, transactions);
                    break;
                default:
                    DryRun(transactions);
                    break;
            }
        }

        private static AppSettings GetConfig()
        {
            var builder = new ConfigurationBuilder().AddJsonFile(ConfigFile).Build();
            var config = new AppSettings();
            builder.Bind(config);

            return config;
        }

        private static async Task<IList<Transaction>> GetTransactions(AppSettings config, int iteration = 0)
        {
            var monzo = new MonzoHelper(config);

            IList<Transaction> transactions;

            try
            {
                using (var client = new MonzoClient(monzo.AccessToken.Value))
                {
                    var accounts = await client.GetAccountsAsync();
                    var selectedAccount = accounts.FirstOrDefault(a => a.Type == AccountType.uk_retail) ?? accounts[0];

                    transactions = await client
                        .GetTransactionsAsync(selectedAccount.Id, "merchant", monzo.PaginationOptions);
                };
            }
            catch (MonzoException)
            {
                if (iteration >= 2)
                    throw new Exception("Unable to refresh access token");

                await monzo.RefreshToken();
                transactions = await GetTransactions(config, ++iteration);
            }

            return transactions;
        }

        private static void ProcessGoogle(AppSettings config, IList<Transaction> transactions)
        {
            var google = new GoogleHelper(config);

            var values = google.BuildList(transactions);

            var response = google.Append(values).Result;

            WriteLine("Finished appending values!");
            WriteLine($"Updated range: {response.Updates.UpdatedRange}");
            WriteLine($"Updated rows: {response.Updates.UpdatedRows}");
            WriteLine($"Updated columns: {response.Updates.UpdatedColumns}");
            WriteLine($"Updated cells: {response.Updates.UpdatedCells}");
        }

        private static void ProcessCsv(AppSettings config, IEnumerable<Transaction> transactions)
        {
            if (string.IsNullOrWhiteSpace(config.CsvExportPath))
            {
                WriteLine("Csv export path is not set, exiting.");
                return;
            }

            var csv = new StringBuilder();
            csv.AppendLine("Date,Payee,Inflow,Description");

            foreach (var transaction in transactions)
            {
                var formattedDate = transaction.Created.ToString("yyyy-MM-dd");
                var amount = (decimal)transaction.Amount / 100;
                var name = transaction.Merchant?.Name
                    ?? transaction.CounterParty?.Name
                    ?? (transaction.Metadata.ContainsKey("pot_id")
                        ? transaction.Metadata["pot_id"] : transaction.Description);
                csv.AppendLine($"{formattedDate},{name},{amount},{transaction.Notes}");
            }

            File.WriteAllText(config.CsvExportPath, csv.ToString());

            WriteLine("All done!");
            WriteLine($"{transactions.Count()} transactions exported to: {config.CsvExportPath}");
        }

        private static void ProcessCsvToEmail(AppSettings config, IEnumerable<Transaction> transactions)
        {
            ProcessCsv(config, transactions);

            if (!File.Exists(config.CsvExportPath))
                return;

            WriteLine("Sending email...");

            var res = SendEmail(config, config.SinceTime);

            File.Delete(config.CsvExportPath);

            WriteLine($"All done! Response {res.StatusDescription}");
        }

        private static void DryRun(IEnumerable<Transaction> transactions)
        {
            WriteLine("Dry run mode enabled.");

            foreach (var transaction in transactions)
            {
                var created = transaction.Created.ToString("G", new CultureInfo("en-GB"));
                WriteLine($"{created} {transaction.Category} {transaction.Amount} {transaction.Description}");
            }
        }

        private static IRestResponse SendEmail(AppSettings config, DateTime? sinceTime)
        {
            var client = new RestClient
            {
                BaseUrl = new Uri("https://api.mailgun.net/v3"),
                Authenticator = new HttpBasicAuthenticator("api", config.EmailApiKey)
            };
            var request = new RestRequest();
            request.AddParameter("domain", config.EmailDomain, ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", config.EmailFromAddress);
            request.AddParameter("to", config.EmailToAddress);
            request.AddParameter("subject", "Monzo Exporter Summary");
            request.AddParameter("text", $"Monzo transactions since {sinceTime:s}");
            request.AddFile("attachment", config.CsvExportPath);
            request.Method = Method.POST;
            return client.Execute(request);
        }
    }
}
