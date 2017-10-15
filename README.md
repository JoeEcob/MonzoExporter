# Monzo Exporter

A quick and dirty script for copying [Monzo](https://monzo.com/) transactions to [Google Sheets](https://www.google.com/sheets/about/).

Intended to be run once per day as a cron job to import the previous days transactions.

## Setup

Add your application keys to a `appsettings.json` file in the project root:

```JSON
{
  "oauthPath": "~/.config/monzo-exporter/",
  "googleClientId": "abc123.apps.googleusercontent.com",
  "googleClientSecret": "abc123",
  "googleSpreadsheetId": "abc123",
  "googleSpreadsheetRange": "Transactions!A2:A",
  "monzoClientId": "abc123",
  "monzoClientSecret": "abc123",
  "monzoRedirectUri": "http://localhost"
}
```

Run the script manually to setup Monzo and Google OAuth configs - both of these need web confirmation.

Add cron entry to run at preferred time.
