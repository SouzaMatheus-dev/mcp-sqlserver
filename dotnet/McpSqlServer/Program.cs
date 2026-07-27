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
            "Antes de consultar dados: 1) ListarChavesEstrangeiras e ListarIndices para entender JOINs, " +
            "2) ListarDependencias para impacto entre objetos, " +
            "3) ResumirBanco para visão geral, " +
            "4) ObterDocumentacaoObjeto para MS_Description, " +
            "5) BuscarColuna/BuscarObjeto para descobrir nomes. " +
            "Use o parâmetro database para trocar de banco no mesmo servidor.";
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
