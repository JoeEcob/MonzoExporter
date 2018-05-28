using System;
using System.Collections.Generic;
using System.Globalization;
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
            MonzoHelper monzo = new MonzoHelper(config);

            IList<Transaction> transactions;

            try
            {
                using (var client = new MonzoClient(monzo.AccessToken.Value))
                {
                    IList<Account> accounts = await client.GetAccountsAsync();

                    transactions = await client
                        .GetTransactionsAsync(accounts[0].Id, expand: "merchant", paginationOptions: monzo.PaginationOptions);
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
            GoogleHelper google = new GoogleHelper(config);

            var values = google.BuildList(transactions);

            var response = google.Append(values).Result;

            Console.WriteLine("Finished appending values!");
            Console.WriteLine($"Updated range: {response.Updates.UpdatedRange}");
            Console.WriteLine($"Updated rows: {response.Updates.UpdatedRows}");
            Console.WriteLine($"Updated columns: {response.Updates.UpdatedColumns}");
            Console.WriteLine($"Updated cells: {response.Updates.UpdatedCells}");
        }

        private static void DryRun(IList<Transaction> transactions)
        {
            Console.WriteLine("Dry run mode enabled.");

            foreach (var transaction in transactions)
            {
                var created = transaction.Created.ToString("G", new CultureInfo("en-GB"));
                Console.WriteLine($"{created} {transaction.Category} {transaction.Amount} {transaction.Description}");
            }
        }
    }
}
