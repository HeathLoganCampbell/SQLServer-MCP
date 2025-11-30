using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using ModelContextProtocol.Server;

[McpServerToolType]
public class SqlServerTools
{
    private static string _connectionString = "";

    public static void SetConnectionString(string cs) => _connectionString = cs;

    private static SqlConnection GetConnection() =>
        new SqlConnection(_connectionString);
    
    public sealed record TableResult(
        string[] Columns,
        List<Dictionary<string, string?>> Rows
    );

    [McpServerTool, Description("Lists user tables in the current SQL Server database.")]
    public static async Task<List<string>> SqlListTables()
    {
        const string sql = @"
            SELECT TABLE_SCHEMA + '.' + TABLE_NAME AS Name
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_SCHEMA, TABLE_NAME;
        ";

        var result = new List<string>();

        using var conn = GetConnection();
        await conn.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection)
                                     .ConfigureAwait(false);

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            result.Add(reader.GetString(0));
        }

        return result;
    }

    [McpServerTool, Description("Returns up to `top` rows from the given table (schema-qualified).")]
    public static async Task<TableResult> SqlPreviewTable(
        string tableName,
        int top = 20
    )
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("tableName is required.", nameof(tableName));

        if (top <= 0) top = 20;

        var sql = $"SELECT TOP (@top) * FROM {tableName};";

        using var conn = GetConnection();
        await conn.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@top", top);

        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection)
                                     .ConfigureAwait(false);

        var columns = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
            columns[i] = reader.GetName(i);

        var rows = new List<Dictionary<string, string?>>();

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var row = new Dictionary<string, string?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i));
                row[columns[i]] = val;
            }
            rows.Add(row);
        }

        return new TableResult(columns, rows);
    }

    [McpServerTool, Description("Executes a read-only SELECT query against SQL Server and returns rows.")]
    public static async Task<TableResult> SqlRunQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("sql is required.", nameof(sql));

        // Very simple safety guard: only allow SELECT
        var trimmed = sql.TrimStart();
        if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase)) // CTEs
        {
            throw new InvalidOperationException("Only read-only SELECT queries are allowed.");
        }

        using var conn = GetConnection();
        await conn.OpenAsync().ConfigureAwait(false);

        using var cmd = new SqlCommand(sql, conn);
        using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection)
                                     .ConfigureAwait(false);

        var columns = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
            columns[i] = reader.GetName(i);

        var rows = new List<Dictionary<string, string?>>();

        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            var row = new Dictionary<string, string?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var val = reader.IsDBNull(i) ? null : Convert.ToString(reader.GetValue(i));
                row[columns[i]] = val;
            }
            rows.Add(row);
        }

        return new TableResult(columns, rows);
    }
}