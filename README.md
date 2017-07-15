# Monzo Exporter

A quick and dirty script for copying [Monzo](https://monzo.com/) transactions to [Google Sheets](https://www.google.com/sheets/about/).

Intended to be run once per day as a cron job to import the previous days transactions.

## Setup

Add your application keys to a `appsettings.json` file in the project root:

```JSON
{
  "google_client_id": "abc123.apps.googleusercontent.com",
  "google_client_secret": "abc123",
  "google_oauth_path": "google_oauth",
  "google_spreadsheet_id": "abc123",
  "google_spreadsheet_range": "Transactions!A2:A",
  "monzo_client_id": "abc123",
  "monzo_client_secret": "abc123",
  "monzo_oauth_path": "monzo_oauth.json",
  "monzo_redirect_uri": "http://localhost"
}
```

Run the script manually to setup Monzo and Google OAuth configs - both of these need web confirmation.

Add cron entry to run at preferred time.
