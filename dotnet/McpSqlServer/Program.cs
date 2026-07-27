using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using McpSqlServer.Services;
using McpSqlServer.Tools;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<McpConfig>();
builder.Services.AddSingleton<SqlExecutor>();
builder.Services.AddSingleton<SqlTools>();
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInstructions =
            "Especialista em SQL Server corporativo (somente leitura). " +
            "Antes de consultar dados: 1) ListarChavesEstrangeiras para entender JOINs, " +
            "2) ObterDocumentacaoObjeto para MS_Description, " +
            "3) BuscarColuna/BuscarObjeto para descobrir nomes. " +
            "Use o parâmetro database para trocar de banco no mesmo servidor.";
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
