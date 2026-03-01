# Schema Overrides Configuration

`schema_overrides.json` allows you to modify the Airtable schema output before it is saved,
without changing the raw data fetched from Airtable. This lets the ETL use a customized
version of the schema that reflects how data is actually stored in PostgreSQL.

## File Location
Place `schema_overrides.json` in the project root alongside `appsettings.json`.

## Format

```json
{
  "overrides": [
    { "table": "TABLE_NAME", "field": "FIELD_NAME", "action": "ACTION", ... }
  ]
}
```

Overrides are applied in the order listed.

## Supported Actions

### exclude
Remove a field from the output entirely.
```json
{ "table": "ARTWORK", "field": "Code (from TYPE)", "action": "exclude" }
```

### setType
Override the reported field type.
```json
{ "table": "ARTWORK_IMAGE", "field": "URL", "action": "setType", "value": "multilineText" }
```

### rename
Rename a field in the output.
```json
{ "table": "ARTWORK", "field": "OLD_NAME", "action": "rename", "value": "NEW_NAME" }
```

### setOptions
Override the options JSON for a field.
```json
{ "table": "SOLD", "field": "PRICE", "action": "setOptions", "value": "{\"precision\":2,\"symbol\":\"$\"}" }
```

### add
Add a virtual field that does not exist in Airtable but should appear in the schema
(e.g. a column that will be created in the target table but not populated by the ETL).
```json
{ "table": "ARTWORK", "field": "NEW_FIELD", "action": "add", "fieldType": "singleLineText" }
```
Optionally include `"options"` for fields that require it:
```json
{ "table": "ARTWORK", "field": "NEW_FIELD", "action": "add", "fieldType": "number", "options": { "precision": 0 } }
```

## Verification
1. Run `dotnet run`
2. Compare output `airtable_schema.txt` to `AirtableToPostgres\airtable_schema.txt`
3. Confirm `Code (from TYPE)` is absent from ARTWORK
4. Confirm `URL` in ARTWORK_IMAGE shows type `multilineText`
5. Confirm all other tables/fields are unchanged
