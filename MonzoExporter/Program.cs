﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

using Mondo;
using MonzoExporter.Models;

namespace MonzoExporter
{
    class Program
    {
        private const string ConfigFile = "appsettings.json";

        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder().AddJsonFile(ConfigFile).Build();

            MonzoHelper monzo = new MonzoHelper(config);

            using (var client = new MondoClient(monzo.AccessToken().Result.Value))
            {
                IList<Account> accounts = client.GetAccountsAsync().Result;
                IList<Transaction> transactions = client
                    .GetTransactionsAsync(accounts[0].Id, expand: "merchant", paginationOptions: monzo.PaginationOptions)
                    .Result;

                Console.WriteLine($"Found {transactions.Count} transactions to add.");

                // TODO - return if no transactions

                GoogleHelper google = new GoogleHelper(config);

                var values = new List<IList<object>>();
                foreach (var item in transactions)
                {
                    var description = item.Merchant?.Name == null ? item.Description : item.Merchant.Name;

                    var cells = new string[] { description, item.Category, item.Amount.ToString(), item.Created.ToString() };
                    var row = new List<object>(cells);

                    values.Add(row);
                    Console.WriteLine($"Added 1 row: {String.Join(" - ", cells)}");
                }

                var response = google.Append(values).Result;

                Console.WriteLine("Finished appending values!");
                Console.WriteLine($"Updated range: {response.Updates.UpdatedRange}");
                Console.WriteLine($"Updated rows: {response.Updates.UpdatedRows}");
                Console.WriteLine($"Updated columns: {response.Updates.UpdatedColumns}");
                Console.WriteLine($"Updated cells: {response.Updates.UpdatedCells}");
            }
        }
    }
}
