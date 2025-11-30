using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

string? connString = builder.Configuration.GetConnectionString("SqlServer");
Debug.Assert(connString != null, nameof(connString) + " != null");
SqlServerTools.SetConnectionString(connString);

builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<SqlServerTools>();
var app = builder.Build();

app.MapMcp();

app.Run("http://localhost:3001");