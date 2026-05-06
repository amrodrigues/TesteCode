using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Data.SqlClient;
using System.Data;
using TesteCode.Database;

namespace TesteCode
{
    public class CriarOrcamentoDto
    {
        public int ClienteId { get; set; }
        public int VeiculoId { get; set; }
        public List<CriarOrcamentoItemDto> Itens { get; set; } = new();
    }

    public class CriarOrcamentoItemDto
    {
        public string Descricao { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
    }

    public static class Program
    {
        private static IConfiguration? _configuration;

        public static void Main(string[] args)
        {
            InicializarConfiguracao();

            // Roda a API Web em segundo plano
            Task.Run(() => RodarApiWeb(args));

            Console.WriteLine("====================================================");
            Console.WriteLine(" API da Oficina iniciada em http://localhost:5000");
            Console.WriteLine("====================================================\n");

            Console.WriteLine("Escolha uma opção:");
            Console.WriteLine("1 - Detector de Palíndromos");
            Console.WriteLine("2 - Gerador da Sequência de Fibonacci");
            Console.WriteLine("3 - Detector e Ajustador de Pontuação");
            Console.WriteLine("4 - Finalizar Orçamento");
            Console.WriteLine("0 - Sair");

            while (true)
            {
                Console.Write("Opção: ");
                string opc = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(opc)) continue;

                if (opc == "0") break;

                switch (opc)
                {
                    case "1":
                        Console.Write("Digite uma palavra ou frase: ");
                        string entrada = Console.ReadLine() ?? string.Empty;
                        bool eh = PalindromeDetector.EhPalindromo(entrada);
                        Console.ForegroundColor = eh ? ConsoleColor.Green : ConsoleColor.Red;
                        Console.WriteLine(eh ? $"-> \"{entrada}\" é um palíndromo!" : $"-> \"{entrada}\" não é um palíndromo.");
                        Console.ResetColor();
                        break;

                    case "2":
                        Console.Write("Digite a quantidade de elementos (X): ");
                        if (int.TryParse(Console.ReadLine(), out int x) && x > 0)
                        {
                            List<int> seq = FibonacciGenerator.GerarFibonacci(x);
                            Console.WriteLine($"\nResultado para X = {x}:");
                            Console.WriteLine(string.Join(", ", seq));
                        }
                        else
                        {
                            Console.WriteLine("Por favor, insira um número inteiro maior que zero.");
                        }
                        break;

                    case "3":
                        Console.Write("Digite o text: ");
                        string texto = Console.ReadLine() ?? string.Empty;
                        string normalizado = TextNormalizer.NormalizarTexto(texto);
                        Console.WriteLine($"\nResultado: {normalizado}");
                        break;

                    case "4":
                        Console.Write("Digite o Id do orçamento: ");
                        if (int.TryParse(Console.ReadLine(), out int orcId))
                        {
                            ExecutarFinalizacaoOrcamento(orcId);
                        }
                        else
                        {
                            Console.WriteLine("Id inválido.");
                        }
                        break;

                    default:
                        Console.WriteLine("Opção inválida.");
                        break;
                }

                Console.WriteLine();
            }
        }

        private static void InicializarConfiguracao()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                _configuration = builder.Build();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso: Não foi possível carregar o arquivo appsettings.json ({ex.Message}).");
            }
        }

        private static void ExecutarFinalizacaoOrcamento(int orcId)
        {
            try
            {
                string? csToUse = Environment.GetEnvironmentVariable("BOOTCAMP_MATERA_CONN");

                if (string.IsNullOrWhiteSpace(csToUse) && _configuration != null)
                {
                    csToUse = _configuration.GetConnectionString("DefaultConnection");
                }

                if (string.IsNullOrWhiteSpace(csToUse))
                {
                    Console.WriteLine("Connection string não informada.");
                    return;
                }

                var res = OrcamentoService.FinalizarOrcamento(csToUse, orcId);
                Console.WriteLine($"Codigo: {res.Codigo}, Mensagem: {res.Mensagem}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao finalizar orçamento: {ex.Message}");
            }
        }

        private static void RodarApiWeb(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();

            var app = builder.Build();

            app.MapPost("/api/orcamento", async (CriarOrcamentoDto dto) =>
            {
                // 1. Validações básicas
                if (dto.ClienteId <= 0)
                    return Results.BadRequest(new { Erro = "O clienteId é obrigatório." });

                if (dto.VeiculoId <= 0)
                    return Results.BadRequest(new { Erro = "O veiculoId é obrigatório." });

                if (dto.Itens == null || !dto.Itens.Any())
                    return Results.BadRequest(new { Erro = "O orçamento deve ter pelo menos 1 item." });

                // 2. Calcula o valor total
                decimal totalGeral = dto.Itens.Sum(i => i.Quantidade * i.ValorUnitario);

                // 3. Recupera a Connection String
                string? cs = _configuration?.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(cs))
                {
                    return Results.Json(new { Erro = "Connection string 'BootcampMatera' não configurada no appsettings.json." }, statusCode: 500);
                }

                try
                {
                    int idGeradoNoBanco = 0;

                    // 4. Conexão e gravação utilizando ADO.NET adaptado para bater exatamente com as suas classes
                    using (var conn = new SqlConnection(cs))
                    {
                        await conn.OpenAsync();
                        using (var transaction = conn.BeginTransaction())
                        {
                            try
                            {
                                // ATENÇÃO: Verifique se sua tabela se chama 'Orcamento' ou 'Orcamentos'
                                // Se for no singular, altere abaixo para "INSERT INTO Orcamento ..."
                                string sqlOrcamento = @"
                                    INSERT INTO Orcamento (ClienteId, VeiculoId, Status, ValorTotal) 
                                    VALUES (@ClienteId, @VeiculoId, 'Aberto', @ValorTotal);
                                    SELECT SCOPE_IDENTITY();";

                                using (var cmd = new SqlCommand(sqlOrcamento, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@ClienteId", dto.ClienteId);
                                    cmd.Parameters.AddWithValue("@VeiculoId", dto.VeiculoId);
                                    cmd.Parameters.AddWithValue("@ValorTotal", totalGeral);

                                    idGeradoNoBanco = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                                }

                                // ATENÇÃO: Verifique se sua tabela de itens se chama 'OrcamentoItem' ou 'OrcamentoItens'
                                string sqlItem = @"
                                    INSERT INTO OrcamentoItem (OrcamentoId, Descricao, Quantidade, ValorUnitario) 
                                    VALUES (@OrcamentoId, @Descricao, @Quantidade, @ValorUnitario);";

                                foreach (var item in dto.Itens)
                                {
                                    using (var cmdItem = new SqlCommand(sqlItem, conn, transaction))
                                    {
                                        cmdItem.Parameters.AddWithValue("@OrcamentoId", idGeradoNoBanco);
                                        cmdItem.Parameters.AddWithValue("@Descricao", item.Descricao);
                                        cmdItem.Parameters.AddWithValue("@Quantidade", item.Quantidade);
                                        cmdItem.Parameters.AddWithValue("@ValorUnitario", item.ValorUnitario);

                                        await cmdItem.ExecuteNonQueryAsync();
                                    }
                                }

                                await transaction.CommitAsync();
                            }
                            catch (Exception dbEx)
                            {
                                await transaction.RollbackAsync();
                                // Mostra o erro do banco no console do VS para você ver o que falhou
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($"[ERRO BANCO DE DADOS]: {dbEx.Message}");
                                Console.ResetColor();
                                throw;
                            }
                        }
                    }

                    return Results.Created($"/api/orcamento/{idGeradoNoBanco}", new
                    {
                        Id = idGeradoNoBanco,
                        ClienteId = dto.ClienteId,
                        VeiculoId = dto.VeiculoId,
                        Status = "Aberto",
                        ValorTotal = totalGeral,
                        Itens = dto.Itens
                    });
                }
                catch (Exception ex)
                {
                    // Força o Postman a exibir o erro detalhado formatado como JSON
                    return Results.Json(new
                    {
                        Erro = "Erro interno ao processar requisição",
                        Detalhes = ex.Message,
                        StackTrace = ex.StackTrace
                    }, statusCode: 500);
                }
            });

            app.Run("http://localhost:5000");
        }
    }
}