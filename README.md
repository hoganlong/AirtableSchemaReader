# Airtable Schema Reader

A .NET utility that reads and displays the complete schema of an Airtable base, including all tables, fields, and data types.

## Purpose

This tool helps you understand the structure of your Airtable base before migrating data to PostgreSQL. It will show you:

- All tables in your base
- All fields (columns) in each table
- Field types and configurations
- Field options (choices for select fields, formulas, etc.)

## Prerequisites

- .NET 10.0 SDK
- Airtable API key (Personal Access Token)
- Airtable Base ID

## Configuration

Update `appsettings.json` with your Airtable credentials:

```json
{
  "Airtable": {
    "ApiKey": "YOUR_AIRTABLE_API_KEY",
    "BaseId": "YOUR_BASE_ID"
  }
}
```

### Getting Your Credentials

1. **API Key**: Create a Personal Access Token at https://airtable.com/create/tokens
   - Give it a name (e.g., "Schema Reader")
   - Add scopes: `schema.bases:read`
   - Add access to your base

2. **Base ID**: Found in your Airtable API documentation
   - Go to https://airtable.com/api
   - Select your base
   - The Base ID looks like `appXXXXXXXXXXXXXX`

## Usage

```bash
cd AirtableSchemaReader
dotnet run
```

## Schema Overrides

You can customize the output without modifying source code using `schema_overrides.json`.
This is useful for making the exported schema match what the ETL expects (e.g. excluding
redundant lookup fields, overriding field types).

Supported actions: `exclude`, `setType`, `rename`, `setOptions`, `add`

See [SCHEMA_OVERRIDES.md](SCHEMA_OVERRIDES.md) for full documentation.

## Output

The tool will:
1. Apply any overrides from `schema_overrides.json` (if present)
2. Display the schema in the console with formatted tables
3. Save a detailed schema report to `airtable_schema.txt`

### Console Output Example

```
================================================================================
AIRTABLE SCHEMA READER
================================================================================
Base ID: appXXXXXXXXXXXXXX

Fetching schema from Airtable...
Found 2 table(s) in this base:

================================================================================
TABLE: Customers
ID: tblXXXXXXXXXXXXXX
================================================================================

  Field Name                     Field Type           Description
  ------------------------------ -------------------- ------------------------------
  Name                           singleLineText
  Email                          email
  Status                         singleSelect
      Choices: Active, Inactive, Pending
  Created Date                   date
```

### File Output

The `airtable_schema.txt` file contains the same information with additional details like field IDs and full options JSON.

## Supported Field Types

The tool recognizes all Airtable field types including:
- Text fields (singleLineText, multilineText, email, url, etc.)
- Number fields (number, currency, percent, rating)
- Date/time fields
- Select fields (singleSelect, multipleSelects)
- Checkbox and other boolean fields
- Attachment fields
- Linked records
- Formula fields
- Rollup and Lookup fields
- And more...

## Next Steps

After running this tool:
1. Review the `airtable_schema.txt` file
2. Use this information to design your PostgreSQL schema
3. Update the `AirtableToPostgres` project with the proper field mappings

## Troubleshooting

**Error: "Failed to fetch schema: 401"**
- Your API key is invalid or expired
- Create a new Personal Access Token with `schema.bases:read` scope

**Error: "Failed to fetch schema: 403"**
- Your token doesn't have permission to access this base
- Add the base to your token's access list

**Error: "Failed to fetch schema: 404"**
- The Base ID is incorrect
- Verify the Base ID from your Airtable API documentation
