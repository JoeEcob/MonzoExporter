# Monzo Exporter

[![Build status](https://ci.appveyor.com/api/projects/status/hvt324v1nkjrtdev/branch/master?svg=true)](https://ci.appveyor.com/project/JoeEcob/monzoexporter/branch/master)

A quick and dirty script for exporting [Monzo](https://monzo.com/) transactions to various formats. Currently supported are:

* [Google Sheets](https://www.google.com/sheets/about/)
* Csv (YNAB 4 format)
* Csv to Email (via [Mailgun](https://www.mailgun.com/))

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
  "monzoRedirectUri": "http://localhost",
  "csvExportPath": "~/monzo-exporter.csv",
  "EmailApiKey": "test123",
  "EmailDomain": "test.mailgun.org",
  "EmailFromAddress": "test@mailgun.com",
  "EmailToAddress": "me@localhost.com"
}
```

Run the script manually to setup Monzo and Google OAuth configs - both of these need web confirmation.

```Shell
# $type is one of ['google', 'csv', 'csv-to-email']
# $sinceTime is optional and is a C#-parsable datetime value, e.g. '2018-01-01T00:00:00'
$ dotnet run $type $sinceTime
```

Add cron entry to run at preferred time.
