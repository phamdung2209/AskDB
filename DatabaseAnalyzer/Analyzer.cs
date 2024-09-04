﻿using DatabaseAnalyzer.Extractors;
using DatabaseAnalyzer.Models;
using GenAI;
using Helper;
using Newtonsoft.Json;
using System.Data;
using System.Globalization;
using System.Text;

namespace DatabaseAnalyzer
{
    public static class Analyzer
    {
        public const short MaxTotalTables = 500;
        public const byte MaxTotalQueries = 50;
        public static List<Table> SelectedTables = new();
        public static DatabaseExtractor DatabaseExtractor;
        private static string SampleData = string.Empty;

        public static string TablesAsString(List<Table> tables)
        {
            var schemas = tables.Select(d => d.ToString()).ToList();
            return string.Join(string.Empty, schemas).Trim();
        }

        public static async Task<SqlCommander> GetSql(string apiKey, string question, DatabaseType type)
        {
            var promptBuilder = new StringBuilder();
            var databaseType = type.ToString();

            promptBuilder.AppendLine($"You are a Database Administrator with over 20 years of experience working with {databaseType} databases on large scale projects.");
            promptBuilder.AppendLine("I am someone who knows nothing about SQL.");
            promptBuilder.AppendLine($"I will provide you the structure of my database with some sample data from my database, and my query in natural language. Please help me convert it into a corresponding {databaseType} query.");
            promptBuilder.AppendLine("Your response must include two parts as follows:");
            promptBuilder.AppendLine($"- Output: This is your response to my input. If my input cannot be converted into a {databaseType} query or you find it is not relevant to the table structure in the database I provided, please respond that my request is invalid and why. Otherwise, please return the corresponding {databaseType} query.");
            promptBuilder.AppendLine("- IsSql: If the Output is an SQL query, this should be TRUE; otherwise, it should be FALSE.");
            promptBuilder.AppendLine("Your response should be a JSON that corresponds to the following C# class:");
            promptBuilder.AppendLine("class SqlCommander");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("    string Output;");
            promptBuilder.AppendLine("    bool IsSql;");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("To help you understand my command and do the task more effectively, here is an example:");
            promptBuilder.AppendLine("My input: give me all available products");
            promptBuilder.AppendLine("Your response:");
            promptBuilder.AppendLine("{");
            promptBuilder.AppendLine("    \"Output\" : \"SELECT * AS AvailableProducts FROM Products WHERE IsAvailable = 1\",");
            promptBuilder.AppendLine("    \"IsSql\" : true");
            promptBuilder.AppendLine("}");
            promptBuilder.AppendLine("Now, let's get started.");
            promptBuilder.AppendLine("This is the table schemas of my database:");
            promptBuilder.AppendLine(TablesAsString(SelectedTables));
            promptBuilder.AppendLine("This is the some sample data from my database:");
            promptBuilder.AppendLine(SampleData);
            promptBuilder.AppendLine($"My input: {question}");
            promptBuilder.AppendLine("Your response:");

            var response = await Generator.GenerateContent(apiKey, promptBuilder.ToString(), true, CreativityLevel.Medium, GenerativeModel.Gemini_15_Flash);
            return JsonConvert.DeserializeObject<SqlCommander>(response);
        }

        public static async Task<List<string>> GetSuggestedQueries(string apiKey, DatabaseType type, bool useSql)
        {
            var promptBuilder = new StringBuilder();
            var databaseType = type.ToString();
            var englishQuery = !useSql ? $"human language ({CultureInfo.CurrentCulture.EnglishName.Split(' ')[0]})" : databaseType;

            promptBuilder.AppendLine($"You are a Database Administrator with over 20 years of experience working with {databaseType} databases on large scale projects.");
            promptBuilder.AppendLine("I am someone who knows nothing about SQL.");
            promptBuilder.AppendLine($"I will provide you with the table structure of my database with some sample data. You have to suggest at least {MaxTotalQueries} common and completely different {englishQuery} queries related to my database structure.");
            promptBuilder.AppendLine("Your response must be a List<string> in C# programming language.");
            promptBuilder.AppendLine("To help you understand my command and do the task more effectively, here is an example:");
            promptBuilder.AppendLine("Your response:");
            promptBuilder.AppendLine("[");
            if (useSql)
            {
                promptBuilder.AppendLine($"    SELECT * FROM Table1 WHERE Condition,");
                promptBuilder.AppendLine($"    SELECT COUNT(*) FROM Table2 WHERE Condition,");
                promptBuilder.AppendLine($"    SELECT DISTINCT(*) FROM Table3 WHERE Condition OrderBy Id DESC");
            }
            else
            {
                promptBuilder.AppendLine($"    Give me all items of the ExampleTableName table,");
                promptBuilder.AppendLine($"    How many ExampleTableName items that <some_conditions>?,");
                promptBuilder.AppendLine($"    I want to know 10 latest item of the ExampleTableName that <some_conditions>,");
                promptBuilder.AppendLine($"    Tell me the items of the ExampleTableName that <some_conditions> after <some_date>");
            }
            promptBuilder.AppendLine("]");
            promptBuilder.AppendLine("Now, let's get started.");
            promptBuilder.AppendLine("This is the table schemas of my database:");
            promptBuilder.AppendLine(TablesAsString(SelectedTables));
            promptBuilder.AppendLine("This is the some sample data from my database:");
            promptBuilder.AppendLine(SampleData);
            promptBuilder.AppendLine("Your response:");

            var response = await Generator.GenerateContent(apiKey, promptBuilder.ToString(), true, CreativityLevel.Medium, GenerativeModel.Gemini_15_Flash);
            return JsonConvert.DeserializeObject<List<string>>(response);
        }

        public static async Task<bool> IsSqlSafe(string sqlCommand)
        {
            string[] unsafeKeywords = {
                "INSERT", "UPDATE", "DELETE", "ALTER", "CREATE", "DROP", "TRUNCATE", "MERGE", "REPLACE", "ADD",
                "MODIFY", "RENAME", "GRANT", "REVOKE", "COMMIT", "ROLLBACK", "SAVEPOINT", "SET", "USE", "LOCK",
                "UNLOCK", "EXPLAIN", "ANALYZE", "OPTIMIZE", "CHECK", "CASCADE", "REFERENCES", "REINDEX", "VACUUM",
                "ENABLE", "DISABLE", "ATTACH", "DETACH", "REPAIR", "REBUILD", "INITIATE", "EXTEND", "SHRINK", "TRANSFER",
                "DISTRIBUTE", "ARCHIVE", "PARTITION", "ADD CONSTRAINT", "DROP CONSTRAINT", "RENAME COLUMN", "ALTER COLUMN",
                "SET DEFAULT", "UNSET DEFAULT", "CONVERT TO", "ALTER INDEX", "CREATE TABLE", "CREATE INDEX", "CREATE VIEW",
                "CREATE PROCEDURE", "CREATE FUNCTION", "CREATE TRIGGER", "CREATE SEQUENCE", "ALTER TABLE", "ALTER VIEW",
                "ALTER PROCEDURE", "ALTER FUNCTION", "ALTER TRIGGER", "ALTER SEQUENCE", "DROP TABLE", "DROP INDEX", "DROP VIEW",
                "DROP PROCEDURE", "DROP FUNCTION", "DROP TRIGGER", "DROP SEQUENCE", "TRUNCATE TABLE", "SET IDENTITY_INSERT",
                "RENAME TABLE", "RENAME INDEX", "RENAME VIEW", "RENAME PROCEDURE", "RENAME FUNCTION", "RENAME TRIGGER",
                "RENAME SEQUENCE", "SET TRANSACTION ISOLATION LEVEL", "BEGIN TRANSACTION", "END TRANSACTION", "BEGIN WORK",
                "END WORK", "CREATE SCHEMA", "DROP SCHEMA", "ALTER SCHEMA", "CREATE USER", "DROP USER", "ALTER USER",
                "CREATE ROLE", "DROP ROLE", "ALTER ROLE", "REVOKE ALL PRIVILEGES", "GRANT ALL PRIVILEGES", "DENY", "REVOKE",
                "CHECK CONSTRAINT", "DISABLE TRIGGER", "ENABLE TRIGGER"
            };

            return await Task.Run(() =>
            {
                var words = StringTool.GetWords(sqlCommand);

                foreach (var word in words)
                {
                    if (unsafeKeywords.Contains(word.ToUpper()))
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        public static async Task PrepareSampleData(short rowsPerTable)
        {
            var sb = new StringBuilder();

            foreach (var table in SelectedTables)
            {
                var exampleDataTable = await DatabaseExtractor.Execute($"SELECT TOP {rowsPerTable} * FROM [{table.Name}] ORDER BY {table.Columns[0].Name} DESC");

                if (exampleDataTable == null || exampleDataTable.Columns.Count == 0 || exampleDataTable.Rows.Count == 0)
                {
                    continue;
                }

                foreach (DataRow row in exampleDataTable.Rows)
                {
                    sb.AppendFormat("INSERT INTO [{0}] (", table.Name);

                    for (int i = 0; i < exampleDataTable.Columns.Count; i++)
                    {
                        sb.Append(exampleDataTable.Columns[i].ColumnName);
                        if (i < exampleDataTable.Columns.Count - 1)
                            sb.Append(", ");
                    }

                    sb.Append(") VALUES (");

                    for (int i = 0; i < exampleDataTable.Columns.Count; i++)
                    {
                        object value = row[i];

                        if (value == DBNull.Value)
                        {
                            sb.Append("NULL");
                        }
                        else if (value is string || value is DateTime)
                        {
                            sb.AppendFormat("'{0}'", value.ToString().Replace("'", "''"));
                        }
                        else
                        {
                            sb.Append(value);
                        }

                        if (i < exampleDataTable.Columns.Count - 1)
                            sb.Append(", ");
                    }

                    sb.Append(");\n");
                }
            }

            SampleData = sb.ToString().Trim();
        }

    }
}
