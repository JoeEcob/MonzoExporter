using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Mondo;
using MonzoExporter.Models;
using System.Threading.Tasks;

namespace MonzoExporter
{
    class Program
    {
        private const string ConfigFile = "appsettings.json";

        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddJsonFile(ConfigFile).Build();

            var transactions = GetTransactions(config).Result;

            if (transactions.Count == 0)
            {
                Console.WriteLine("No transactions found, exiting.");
                return;
            }

            Console.WriteLine($"Found {transactions.Count} transactions to add.");

            switch ((args.Length > 0 ? args[0] : "").ToLower())
            {
                case "dry-run":
                    DryRun(transactions);
                    break;
                default:
                    ProcessGoogle(config, transactions);
                    break;
            }
        }

        private static async Task<IList<Transaction>> GetTransactions(IConfiguration config, int iteration = 0)
        {
            MonzoHelper monzo = new MonzoHelper(config);

            IList<Transaction> transactions;

            try
            {
                using (var client = new MondoClient(monzo.AccessToken.Value))
                {
                    IList<Account> accounts = await client.GetAccountsAsync();

                    transactions = await client
                        .GetTransactionsAsync(accounts[0].Id, expand: "merchant", paginationOptions: monzo.PaginationOptions);
                };
            }
            catch (MondoException)
            {
                if (iteration >= 2)
                    throw new Exception("Unable to refresh access token");

                await monzo.RefreshToken();
                transactions = await GetTransactions(config, ++iteration);
            }

            return transactions;
        }

        private static void ProcessGoogle(IConfiguration config, IList<Transaction> transactions)
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
                Console.WriteLine($"{transaction.Created} {transaction.Category} {transaction.Amount} {transaction.Description}");
            }
        }
    }
}
