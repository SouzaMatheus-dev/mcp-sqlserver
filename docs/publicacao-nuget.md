# Publicação NuGet

Guia para publicar e consumir o pacote **McpSqlServer** no NuGet.org.

---

## Pacote

| Campo | Valor |
|---|---|
| **PackageId** | `McpSqlServer` |
| **Tipo** | .NET Global Tool (`DotnetTool`) |
| **Comando CLI** | `mcp-sqlserver` |
| **Target** | `net8.0` (Windows) |

O pacote expõe um servidor MCP para SQL Server com as mesmas variáveis de ambiente
e ferramentas documentadas neste repositório.

---

## Consumir (usuário final)

### Instalar do NuGet.org

```powershell
dotnet tool install --global McpSqlServer
```

### Instalar versão específica

```powershell
dotnet tool install --global McpSqlServer --version 0.2.0
```

### Instalar de feed privado (Azure Artifacts / GitHub Packages)

```powershell
dotnet nuget add source "https://pkgs.dev.azure.com/SUA-ORG/_packaging/SUA-FEED/nuget/v3/index.json" `
  --name "empresa-interno" `
  --username "SUA-ORG" `
  --password "$env:AZURE_DEVOPS_PAT"

dotnet tool install --global McpSqlServer `
  --add-source "empresa-interno"
```

### Configurar no Gemini após instalar

```json
{
  "mcpServers": {
    "sqlserver-hml": {
      "command": "mcp-sqlserver",
      "env": {
        "MSSQL_SERVER": "SRVSQL01\\HML",
        "MSSQL_DATABASE": "master",
        "MSSQL_READONLY": "true"
      }
    }
  }
}
```

---

## Publicar (mantenedor)

### Pré-requisitos

- .NET 8 SDK
- Conta no [nuget.org](https://www.nuget.org/)
- API Key do NuGet (`NUGET_API_KEY`)

### Build e pack

> O projeto .NET está em [`dotnet/`](../dotnet/). Quando implementado:

```powershell
cd dotnet/McpSqlServer
dotnet pack -c Release -o ./nupkg
```

Gera: `nupkg/McpSqlServer.0.2.0.nupkg`

### Publicar no NuGet.org

```powershell
dotnet nuget push ./nupkg/McpSqlServer.0.2.0.nupkg `
  --api-key $env:NUGET_API_KEY `
  --source https://api.nuget.org/v3/index.json
```

### Publicar no GitHub Packages

```powershell
dotnet nuget push ./nupkg/McpSqlServer.0.2.0.nupkg `
  --api-key $env:GITHUB_TOKEN `
  --source "https://nuget.pkg.github.com/SouzaMatheus-dev/index.json"
```

---

## CI/CD (GitHub Actions)

Exemplo de workflow para publicar ao criar tag `v*`:

```yaml
name: Publish NuGet

on:
  push:
    tags:
      - 'v*'

jobs:
  publish:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Pack
        run: dotnet pack dotnet/McpSqlServer/McpSqlServer.csproj -c Release -o ./nupkg

      - name: Push to NuGet
        run: dotnet nuget push ./nupkg/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json
```

---

## Versionamento

Siga [Semantic Versioning](https://semver.org/):

| Versão | Quando usar |
|---|---|
| `0.x.y` | Alpha/beta — API pode mudar |
| `1.0.0` | Primeira versão estável para produção corporativa |
| `1.x.y` | Novas ferramentas, correções |
| `2.0.0` | Breaking changes na config ou ferramentas |

Atualize a versão em:

- `dotnet/McpSqlServer/McpSqlServer.csproj` → `<Version>`
- Tag git: `git tag v0.2.0 && git push origin v0.2.0`

---

## Checklist antes de publicar

- [ ] Testes passando (`dotnet test`)
- [ ] Versão atualizada em todos os arquivos
- [ ] README e docs revisados
- [ ] Tag git criada (`v0.2.0`)
- [ ] Release notes no GitHub
- [ ] API Key NuGet configurada no CI
