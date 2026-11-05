# Postman Tools

This folder contains a lightweight Postman export generator for the local services in this solution.

## Purpose

Create a ready-to-import Postman collection with prepared requests and basic automated tests for:

- KYC Aggregation API
- Logging Service API

## Files

- `generate-postman-collection.ps1` - builds a Postman v2.1 collection JSON file
- `KYCAggrService.PostmanCollection.json` - generated output file

## Run it

From the repo root:

```powershell
pwsh ./tools/postman/generate-postman-collection.ps1
```

Or in Windows PowerShell:

```powershell
./tools/postman/generate-postman-collection.ps1
```

## Included requests

- KYC health check
- KYC happy path request
- KYC invalid input request
- Logging service health check
- Logging recent logs query
- Logging event POST with example payload

## Notes

The collection is designed for local development and uses the default local URLs:

- KYC API: `http://localhost:5180`
- Logging API: `http://localhost:5210`

You can import the generated JSON into Postman and run the requests against the local services.
