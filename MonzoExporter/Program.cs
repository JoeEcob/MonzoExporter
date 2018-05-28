﻿using System;
using static System.Console;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;

using Monzo;
using MonzoExporter.Helpers;
using System.Threading.Tasks;
using MonzoExporter.Models;

namespace MonzoExporter
{
    class Program
    {
        private const string ConfigFile = "appsettings.json";

        static void Main(string[] args)
        {
            var config = GetConfig();

            var transactions = GetTransactions(config).Result;

            if (transactions.Count == 0)
            {
                Console.WriteLine("No transactions found, exiting.");
                return;
            }

            Console.WriteLine($"Found {transactions.Count} transactions to add.");

            switch ((args.Length > 0 ? args[0] : "").ToLower())
            {
                case "google":
                    ProcessGoogle(config, transactions);
                    break;
                case "csv":
                    ProcessCsv(config, transactions);
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
                csv.AppendLine($"{formattedDate},{transaction.Merchant.Name},{amount},{transaction.Notes}");
            }

            File.WriteAllText(config.CsvExportPath, csv.ToString());

            WriteLine("All done!");
            WriteLine($"{transactions.Count()} transactions exported to: {config.CsvExportPath}");
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
    }
}
