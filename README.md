# 🛠️ Oficina Mecânica & Utilitários em .NET

Este repositório foi desenvolvido como parte de um teste técnico prático. Ele consiste em uma aplicação **.NET** integrada que combina uma interface de **Console interativa** e uma **Web API (Minimal API)** rodando simultaneamente em segundo plano.

A aplicação realiza tarefas utilitárias e se conecta a um banco de dados **SQL Server (LocalDB)** para manipulação de orçamentos e execução de **Stored Procedures**.

---

## 🚀 Funcionalidades do Projeto

O projeto é dividido em duas frentes que rodam simultaneamente:

### 1. Menu Interativo de Console
* **Detector de Palíndromos:** Analisa se uma frase ou palavra lida de trás para frente é idêntica à original, tratando maiúsculas/minúsculas e acentuação.
* **Gerador de Sequência de Fibonacci:** Gera uma lista contendo os primeiros $X$ elementos da sequência com validações de entrada do usuário.
* **Detector e Ajustador de Pontuação:** Normaliza textos corrigindo espaçamentos antes e depois de pontuações.
* **Finalizar Orçamento (Integração com SQL Server):** Executa a stored procedure `sp_FinalizarOrcamento` utilizando ADO.NET (`Microsoft.Data.SqlClient`) de forma assíncrona, com tratamento robusto de parâmetros de entrada e saída (`OUTPUT`).

<img width="957" height="503" alt="Testecode1" src="https://github.com/user-attachments/assets/ebc48ffc-1706-4c01-b011-9fffe7650824" />

### 2. Web API (Minimal API)
* **Cadastro de Orçamentos (`POST /api/orcamento`):**
    * Recebe um payload JSON com as informações do cliente, veículo e itens.
    * **Validação de Regras de Negócio:** Garante que o cliente e veículo sejam válidos, que exista pelo menos 1 item e que as quantidades e valores sejam estritamente maiores que zero.
    * **Cálculo Automático:** O valor total de cada item e o valor total geral do orçamento são calculados de forma segura pelo servidor, prevenindo fraudes ou inconsistências no envio do JSON.



---

## 📂 Estrutura de Arquivos Principal

* **`Program.cs`**: Ponto de entrada que inicializa a configuração (`appsettings.json`), sobe a Web API de forma assíncrona em segundo plano (porta `5000`) e gerencia o menu de Console na thread principal.
* **`OrcamentoService.cs`**: Camada de persistência/serviço responsável por se conectar ao banco de dados e executar a procedure de finalização utilizando `SqlConnection` e `SqlCommand` de forma assíncrona (`async/await`).
* **`appsettings.json`**: Arquivo de configuração que gerencia a string de conexão de forma segura e flexível.

---

## 🔧 Configuração e Banco de Dados

O projeto está configurado para utilizar o **SQL Server LocalDB** (instalado nativamente com o Visual Studio), eliminando a necessidade de configurar instâncias completas ou portas de rede complexas para testes locais.

### 1. Connection String (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=BootCampMatera;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
  }
}
```
## Criação das tabelas 

```bash -- Criação da tabela pai: Orcamento
CREATE TABLE Orcamento (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ClienteId INT NOT NULL,
    VeiculoId INT NOT NULL,
    Status VARCHAR(50) NOT NULL DEFAULT 'Aberto',
    ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    DataCriacao DATETIME NOT NULL DEFAULT GETDATE(),
    DataFinalizacao DATETIME NULL
);

-- Criação da tabela filho: OrcamentoItem
CREATE TABLE OrcamentoItem (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrcamentoId INT NOT NULL,
    Descricao VARCHAR(255) NOT NULL,
    Quantidade INT NOT NULL,
    ValorUnitario DECIMAL(18,2) NOT NULL,
    ValorTotal DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    CONSTRAINT FK_OrcamentoItem_Orcamento FOREIGN KEY (OrcamentoId) 
        REFERENCES Orcamento(Id) ON DELETE CASCADE
);
```
### StoredProcedure
```bash
CREATE PROCEDURE sp_FinalizarOrcamento
    @OrcamentoId INT,
    @CodigoRetorno INT OUTPUT, -- 1 para Sucesso, menor que 0 para Erros específicos
    @MensagemRetorno VARCHAR(255) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Declaração de variáveis de validação
    DECLARE @StatusAtual VARCHAR(50);
    DECLARE @ValorTotalCalculado DECIMAL(18,2);
    DECLARE @QuantidadeItens INT;

    -- 1. Verifica se o orçamento existe e obtém seu status atual
    SELECT @StatusAtual = Status 
    FROM Orcamento 
    WHERE Id = @OrcamentoId;

    IF @StatusAtual IS NULL
    BEGIN
        SET @CodigoRetorno = -1;
        SET @MensagemRetorno = 'Erro: Orçamento não encontrado.';
        RETURN;
    END;

    -- 2. Verifica se o orçamento ainda está com status 'Aberto'
    IF UPPER(@StatusAtual) <> 'ABERTO'
    BEGIN
        SET @CodigoRetorno = -2;
        SET @MensagemRetorno = 'Erro: O orçamento não está com o status Aberto (Status atual: ' + @StatusAtual + ').';
        RETURN;
    END;

    -- 3. Verifica se o orçamento possui pelo menos 1 item e já calcula o valor total
    SELECT 
        @QuantidadeItens = COUNT(Id),
        @ValorTotalCalculado = SUM(Quantidade * ValorUnitario)
    FROM OrcamentoItem
    WHERE OrcamentoId = @OrcamentoId;

    IF @QuantidadeItens IS NULL OR @QuantidadeItens = 0
    BEGIN
        SET @CodigoRetorno = -3;
        SET @MensagemRetorno = 'Erro: O orçamento não possui itens cadastrados.';
        RETURN;
    END;

    -- Início do processo de atualização de forma segura (Transacional)
    BEGIN TRANSACTION;

    BEGIN TRY
        -- Atualiza os itens garantindo que a coluna calculada 'ValorTotal' de cada item esteja correta
        UPDATE OrcamentoItem
        SET ValorTotal = Quantidade * ValorUnitario
        WHERE OrcamentoId = @OrcamentoId;

        -- 4, 5 e 6. Atualiza o cabeçalho do Orçamento com o valor recalculado, novo status e data
        UPDATE Orcamento
        SET 
            ValorTotal = @ValorTotalCalculado,
            Status = 'Finalizado',
            DataFinalizacao = GETDATE()
        WHERE Id = @OrcamentoId;

        -- Se tudo deu certo, confirma as alterações no banco
        COMMIT TRANSACTION;

        SET @CodigoRetorno = 1;
        SET @MensagemRetorno = 'Orçamento finalizado com sucesso. Valor total recalculado: R$ ' + CONVERT(VARCHAR, @ValorTotalCalculado);
    END TRY
    BEGIN CATCH
        -- Se houver qualquer falha física ou lógica no bloco acima, desfaz as alterações
        ROLLBACK TRANSACTION;

        SET @CodigoRetorno = -99;
        SET @MensagemRetorno = 'Erro interno ao processar: ' + ERROR_MESSAGE();
    END CATCH;
END;
```

## 🧪 Como Testar a API (Cadastro de Orçamentos)

Com a aplicação rodando, envie uma requisição do tipo POST para http://localhost:5000/api/orcamento.

<img width="919" height="695" alt="Testecode2" src="https://github.com/user-attachments/assets/1da89c85-1c84-4706-8fe4-b2c1f2d2b116" />

Corpo da Requisição (POST)

```json
{
  "clienteId": 10,
  "veiculoId": 25,
  "itens": [
    {
      "descricao": "Troca de óleo",
      "quantidade": 1,
      "valorUnitario": 120.00
    },
    {
      "descricao": "Filtro de óleo",
      "quantidade": 1,
      "valorUnitario": 45.00
    }
  ]
}
```
Resposta Esperada (201 Created)
```json
{
  "id": 184,
  "clienteId": 10,
  "veiculoId": 25,
  "status": "Aberto",
  "valorTotal": 165.00,
  "itens": [
    {
      "descricao": "Troca de óleo",
      "quantidade": 1,
      "valorUnitario": 120.00
    },
    {
      "descricao": "Filtro de óleo",
      "quantidade": 1,
      "valorUnitario": 45.00
    }
  ]
}
```
## 🛠️ Tecnologias Utilizadas
Linguagem: C# (.NET 8.0)

Banco de Dados: Microsoft SQL Server (LocalDB)

Acesso a Dados: ADO.NET nativo (Microsoft.Data.SqlClient) com suporte a execução assíncrona (Async).

Framework Web: ASP.NET Core Minimal APIs

Configuração: Microsoft.Extensions.Configuration para leitura dinâmica de arquivos JSON.
