# McpSqlServer (.NET)

Pacote NuGet global tool — **em desenvolvimento**.

Este diretório receberá o projeto C# equivalente ao pacote Python, com:

- Comando CLI: `mcp-sqlserver`
- Autenticação Windows (`Integrated Security=true`)
- Mesmas variáveis de ambiente (`MSSQL_SERVER`, `MSSQL_READONLY`, etc.)
- Mesmas 11 ferramentas MCP

## Estrutura prevista

```
dotnet/McpSqlServer/
├── McpSqlServer.csproj      # DotnetTool, PackAsTool
├── Program.cs
├── Tools/                   # Ferramentas MCP
├── Services/                # Conexão SQL, validador read-only
└── README.md
```

## Publicação (quando pronto)

```powershell
cd dotnet/McpSqlServer
dotnet pack -c Release -o ./nupkg
dotnet nuget push ./nupkg/McpSqlServer.0.2.0.nupkg --source https://api.nuget.org/v3/index.json
```

Veja o guia completo: [docs/publicacao-nuget.md](../docs/publicacao-nuget.md)
