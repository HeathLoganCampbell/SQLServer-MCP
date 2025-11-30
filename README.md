### SqlServer-MCP

A lightweight Model Context Protocol (MCP) server that exposes safe, read-focused tools for exploring a Microsoft SQL Server database over HTTP. It’s intended to be consumed by MCP-compatible clients and agents.

---

### Features
- Exposes SQL Server tooling via MCP:
  - `SqlListTables`: List user tables (`schema.table`).
  - `SqlPreviewTable`: Preview up to `top` rows from a table.
  - `SqlRunQuery`: Execute read-only `SELECT` (and `WITH` CTE) queries.
- HTTP transport hosted by ASP.NET Core, default: `http://localhost:3001`.
- Connection string configured via `appsettings.json` or environment variables.

---

### Learnings
#### Best way to represent schemas to GPT?
Use a markdown file, likely one for each table
```
<SCHEMA>
tables:
  Employees:
    columns:
      id: int (PK)
      name: string
      department_id: int (FK → Departments.id)
      status: enum["ACTIVE","INACTIVE"]
</SCHEMA>
```

#### Schema Prompting
https://medium.com/@nikunj.agarwal012/schema-prompting-a-revolutionary-approach-to-querying-databases-b950954ccc62
* Schema prompting involves giving a language model (LLM) a formal description of a database schema (tables, columns, types, relationships) along with the user’s natural-language request
* Lowers barrier to entry - non-SQL-savvy users or analysts can query the database by asking in natural language
* Speeds up query generation - developers and analysts save time compared to writing SQL by hand.

1. Schema definition - listing tables, columns, types, relationships (Done by the developer)
2. User question - what data the user wants, in natural language. (Done by a user)
3. Instruction to generate SQL - telling the model to output a SQL query based on the schema + question. (Done by a LLM)
4. Executed SQL - Run the SQL against the database and respond to the user with the response. (Done by this project)
---

### Project layout
- `SqlServer-MCP/Program.cs`: Configures the web host, reads the connection string, registers MCP server and tools, maps MCP endpoint, and runs at `http://localhost:3001`.
- `SqlServer-MCP/SqlServerTools.cs`: Implements the MCP tools for SQL Server (listed above).
- `SqlServer-MCP/appsettings.json`: Holds `ConnectionStrings:SqlServer`.
- `SqlServer-MCP/SqlServer-MCP.csproj`: .NET project config and package references.

---

### Prerequisites
- .NET SDK `9.0` or later
- Access to a Microsoft SQL Server instance
- An MCP-compatible client if you want to consume the tools via MCP (e.g., MCP-enabled IDEs/agents)

---

### Configuration
Set the connection string using one of the following methods.

1) `appsettings.json` (default)
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=localhost;Database=YourDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

2) Environment variable (overrides file)
- Key: `ConnectionStrings__SqlServer`
- Example:
  - Windows (PowerShell):
    ```powershell
    $env:ConnectionStrings__SqlServer = "Server=localhost;Database=YourDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
    ```
  - Linux/macOS (bash):
    ```bash
    export ConnectionStrings__SqlServer="Server=localhost;Database=YourDb;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
    ```

Notes:
- For local dev with self-signed certs, `TrustServerCertificate=True` can be convenient.
- For Windows Integrated Auth, use `Trusted_Connection=True`.

---

### Build and run
From the repository root:

```bash
# Build
dotnet build

# Run (Debug)
dotnet run --project SqlServer-MCP

# Or run on a specific URL
dotnet run --project SqlServer-MCP --urls "http://localhost:3001"
```

On start, the server listens on `http://localhost:3001` and exposes the MCP endpoint via HTTP transport.

---

### Using the MCP tools
This server is designed to be consumed by MCP clients. After connecting your MCP client to `http://localhost:3001`, you can invoke the following tools:

- `SqlListTables()`
  - Description: Lists user tables in the current SQL Server database.
  - Returns: `List<string>` like `schema.table`.

- `SqlPreviewTable(string tableName, int top = 20)`
  - Description: Returns up to `top` rows from the given `schema.table`.
  - Returns: `TableResult` with `Columns` and collection of row dictionaries.

- `SqlRunQuery(string sql)`
  - Description: Executes a read-only SELECT query (or `WITH` CTE). Non-SELECT is blocked.
  - Returns: `TableResult` as above.

Security note: `SqlRunQuery` intentionally rejects non-SELECT statements.

---

### Troubleshooting
- Startup assertion: `Assertion failed. connString != null`
  - Cause: No connection string found.
  - Fixes:
    - Ensure `appsettings.json` exists and is copied to output, the project already includes:
      ```xml
      <ItemGroup>
        <Content Include="appsettings.json">
          <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        </Content>
      </ItemGroup>
      ```
    - Confirm the key path is exactly `ConnectionStrings` → `SqlServer`.
    - Provide via environment variable `ConnectionStrings__SqlServer` if preferred.

- SQL connectivity errors:
  - Verify server hostname/port, credentials, and network access.
  - If using containers or remote SQL Server, make sure ports are exposed and reachable.

---

### Packages
- `ModelContextProtocol` and `ModelContextProtocol.AspNetCore` (`0.4.1-preview.1`)
- `Microsoft.Data.SqlClient` (`7.0.0-preview2.25289.6`)
- `Microsoft.SqlServer.Server` (`1.0.0`)
- Pagination and filtering for previews
- Optional parameterized query support for safe templates
- Additional metadata tools (indexes, foreign keys, etc.)
