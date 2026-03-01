using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var apiKey = configuration["Airtable:ApiKey"];
        var baseId = configuration["Airtable:BaseId"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(baseId))
        {
            Console.WriteLine("Error: Please configure your Airtable API Key and Base ID in appsettings.json");
            return;
        }

        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine("AIRTABLE SCHEMA READER");
        Console.WriteLine("=".PadRight(80, '='));
        Console.WriteLine($"Base ID: {baseId}");
        Console.WriteLine();

        var overrides = LoadOverrides("schema_overrides.json");

        try
        {
            var schema = await FetchBaseSchema(apiKey, baseId);
            ApplyOverrides(schema, overrides);
            DisplaySchema(schema);

            // Also save to file
            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "airtable_schema.txt");
            await SaveSchemaToFile(schema, outputPath);
            Console.WriteLine();
            Console.WriteLine($"Schema saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner error: {ex.InnerException.Message}");
            }
        }
    }

    static async Task<JObject> FetchBaseSchema(string apiKey, string baseId)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var url = $"https://api.airtable.com/v0/meta/bases/{baseId}/tables";

        Console.WriteLine("Fetching schema from Airtable...");
        var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch schema: {response.StatusCode} - {errorContent}");
        }

        var content = await response.Content.ReadAsStringAsync();
        return JObject.Parse(content);
    }

    static void DisplaySchema(JObject schema)
    {
        var tables = schema["tables"] as JArray;

        if (tables == null || !tables.Any())
        {
            Console.WriteLine("No tables found in this base.");
            return;
        }

        Console.WriteLine($"Found {tables.Count} table(s) in this base:\n");

        foreach (var table in tables)
        {
            var tableId = table["id"]?.ToString();
            var tableName = table["name"]?.ToString();
            var description = table["description"]?.ToString();

            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine($"TABLE: {tableName}");
            Console.WriteLine($"ID: {tableId}");
            if (!string.IsNullOrEmpty(description))
            {
                Console.WriteLine($"Description: {description}");
            }
            Console.WriteLine("=".PadRight(80, '='));
            Console.WriteLine();

            var fields = table["fields"] as JArray;

            if (fields == null || !fields.Any())
            {
                Console.WriteLine("  No fields found in this table.\n");
                continue;
            }

            Console.WriteLine($"  {"Field Name",-30} {"Field Type",-20} {"Description",-30}");
            Console.WriteLine($"  {"-".PadRight(30, '-')} {"-".PadRight(20, '-')} {"-".PadRight(30, '-')}");

            foreach (var field in fields)
            {
                var fieldId = field["id"]?.ToString();
                var fieldName = field["name"]?.ToString();
                var fieldType = field["type"]?.ToString();
                var fieldDescription = field["description"]?.ToString() ?? "";

                Console.WriteLine($"  {fieldName,-30} {fieldType,-20} {fieldDescription,-30}");

                // Display additional field options if available
                var options = field["options"];
                if (options != null && options.HasValues)
                {
                    DisplayFieldOptions(options, fieldType);
                }
            }

            Console.WriteLine();
        }
    }

    static void DisplayFieldOptions(JToken options, string fieldType)
    {
        // Handle different field types with options
        switch (fieldType)
        {
            case "singleSelect":
            case "multipleSelects":
                var choices = options["choices"] as JArray;
                if (choices != null && choices.Any())
                {
                    Console.WriteLine($"      Choices: {string.Join(", ", choices.Select(c => c["name"]?.ToString()))}");
                }
                break;

            case "formula":
                var formula = options["formula"]?.ToString();
                if (!string.IsNullOrEmpty(formula))
                {
                    Console.WriteLine($"      Formula: {formula}");
                }
                break;

            case "rollup":
                var fieldId = options["fieldIdInLinkedTable"]?.ToString();
                var recordLinkFieldId = options["recordLinkFieldId"]?.ToString();
                Console.WriteLine($"      Rollup configuration: Field={fieldId}, Link={recordLinkFieldId}");
                break;

            case "lookup":
                var lookupFieldId = options["fieldIdInLinkedTable"]?.ToString();
                var lookupRecordLinkFieldId = options["recordLinkFieldId"]?.ToString();
                Console.WriteLine($"      Lookup configuration: Field={lookupFieldId}, Link={lookupRecordLinkFieldId}");
                break;
        }
    }

    static async Task SaveSchemaToFile(JObject schema, string outputPath)
    {
        var sb = new StringBuilder();
        var tables = schema["tables"] as JArray;

        sb.AppendLine("AIRTABLE SCHEMA EXPORT");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("=".PadRight(80, '='));
        sb.AppendLine();

        if (tables == null || !tables.Any())
        {
            sb.AppendLine("No tables found.");
            await File.WriteAllTextAsync(outputPath, sb.ToString());
            return;
        }

        foreach (var table in tables)
        {
            var tableName = table["name"]?.ToString();
            var tableId = table["id"]?.ToString();
            var description = table["description"]?.ToString();

            sb.AppendLine($"TABLE: {tableName}");
            sb.AppendLine($"ID: {tableId}");
            if (!string.IsNullOrEmpty(description))
            {
                sb.AppendLine($"Description: {description}");
            }
            sb.AppendLine("-".PadRight(80, '-'));

            var fields = table["fields"] as JArray;

            if (fields != null && fields.Any())
            {
                foreach (var field in fields)
                {
                    var fieldName = field["name"]?.ToString();
                    var fieldType = field["type"]?.ToString();
                    var fieldId = field["id"]?.ToString();
                    var fieldDescription = field["description"]?.ToString();

                    sb.AppendLine($"  Field: {fieldName}");
                    sb.AppendLine($"    ID: {fieldId}");
                    sb.AppendLine($"    Type: {fieldType}");
                    if (!string.IsNullOrEmpty(fieldDescription))
                    {
                        sb.AppendLine($"    Description: {fieldDescription}");
                    }

                    var options = field["options"];
                    if (options != null && options.HasValues)
                    {
                        sb.AppendLine($"    Options: {options.ToString(Newtonsoft.Json.Formatting.None)}");
                    }
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
        }

        await File.WriteAllTextAsync(outputPath, sb.ToString());
    }

    static JArray LoadOverrides(string path)
    {
        if (!File.Exists(path))
            return new JArray();

        var json = File.ReadAllText(path);
        var obj = JObject.Parse(json);
        return obj["overrides"] as JArray ?? new JArray();
    }

    static void ApplyOverrides(JObject schema, JArray overrides)
    {
        if (!overrides.Any())
            return;

        var tables = schema["tables"] as JArray;
        if (tables == null)
            return;

        foreach (var entry in overrides)
        {
            var tableName = entry["table"]?.ToString();
            var fieldName = entry["field"]?.ToString();
            var action = entry["action"]?.ToString();

            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(action))
                continue;

            var table = tables.FirstOrDefault(t => t["name"]?.ToString() == tableName);
            if (table == null)
            {
                Console.WriteLine($"  [Override] Warning: table '{tableName}' not found.");
                continue;
            }

            var fields = table["fields"] as JArray;
            if (fields == null)
                continue;

            if (action == "add")
            {
                var newField = new JObject
                {
                    ["name"] = fieldName,
                    ["type"] = entry["fieldType"]?.ToString() ?? "singleLineText"
                };
                var options = entry["options"];
                if (options != null && options.Type != JTokenType.Null)
                    newField["options"] = options;
                fields.Add(newField);
                Console.WriteLine($"  [Override] Added field '{fieldName}' to {tableName}.");
                continue;
            }

            var field = fields.FirstOrDefault(f => f["name"]?.ToString() == fieldName);
            if (field == null)
            {
                Console.WriteLine($"  [Override] Warning: field '{fieldName}' not found in table '{tableName}'.");
                continue;
            }

            switch (action)
            {
                case "exclude":
                    fields.Remove(field);
                    Console.WriteLine($"  [Override] Excluded field '{fieldName}' from {tableName}.");
                    break;
                case "setType":
                    field["type"] = entry["value"]?.ToString();
                    Console.WriteLine($"  [Override] Set type of '{fieldName}' in {tableName} to '{entry["value"]}'.");
                    break;
                case "rename":
                    field["name"] = entry["value"]?.ToString();
                    Console.WriteLine($"  [Override] Renamed '{fieldName}' in {tableName} to '{entry["value"]}'.");
                    break;
                case "setOptions":
                    var optionsStr = entry["value"]?.ToString();
                    field["options"] = optionsStr != null ? JObject.Parse(optionsStr) : JValue.CreateNull();
                    Console.WriteLine($"  [Override] Set options of '{fieldName}' in {tableName}.");
                    break;
                default:
                    Console.WriteLine($"  [Override] Warning: unknown action '{action}'.");
                    break;
            }
        }
    }
}
