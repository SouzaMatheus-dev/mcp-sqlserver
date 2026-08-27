-- Schema mínimo para validar ferramentas MCP contra SQL Server real.
IF OBJECT_ID('dbo.Pedidos', 'U') IS NOT NULL DROP TABLE dbo.Pedidos;
IF OBJECT_ID('dbo.Clientes', 'U') IS NOT NULL DROP TABLE dbo.Clientes;
GO

CREATE TABLE dbo.Clientes (
    Id INT NOT NULL PRIMARY KEY,
    Nome NVARCHAR(100) NOT NULL
);
GO

CREATE TABLE dbo.Pedidos (
    Id INT NOT NULL PRIMARY KEY,
    ClienteId INT NOT NULL,
    Status CHAR(1) NOT NULL,
    CONSTRAINT FK_Pedidos_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id)
);
GO

INSERT INTO dbo.Clientes (Id, Nome) VALUES (1, N'Cliente Teste');
INSERT INTO dbo.Pedidos (Id, ClienteId, Status) VALUES (1, 1, 'A'), (2, 1, 'B');
GO

CREATE INDEX IX_Pedidos_Status ON dbo.Pedidos (Status);
CREATE INDEX IX_Pedidos_Status_Dup ON dbo.Pedidos (Status);
GO

CREATE VIEW dbo.vwPedidos AS
SELECT p.Id, p.ClienteId, p.Status, c.Nome
FROM dbo.Pedidos p
INNER JOIN dbo.Clientes c ON c.Id = p.ClienteId;
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Tabela de pedidos para testes MCP',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Pedidos';
GO
