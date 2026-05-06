using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace TesteCode.Database
{
    public class ProcResult
    {
        public int Codigo { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public static class OrcamentoService
    {
        /// <summary>
        /// Executa assincronamente a stored procedure `sp_FinalizarOrcamento` na base informada.
        /// </summary>
        public static async Task<ProcResult> FinalizarOrcamentoAsync(string connectionString, int orcamentoId)
        {
            if (string.IsNullOrWhiteSpace(connectionString)) 
                throw new ArgumentException("connectionString não informado", nameof(connectionString));

            using var conn = new SqlConnection(connectionString);
            using var cmd = conn.CreateCommand();

            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "sp_FinalizarOrcamento";

            // Entrada
            cmd.Parameters.Add(new SqlParameter("@OrcamentoId", SqlDbType.Int) { Value = orcamentoId });

            // Saídas
            var pCodigo = new SqlParameter("@CodigoRetorno", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var pMensagem = new SqlParameter("@MensagemRetorno", SqlDbType.VarChar, 255) { Direction = ParameterDirection.Output };

            cmd.Parameters.Add(pCodigo);
            cmd.Parameters.Add(pMensagem);

            // Uso do modelo assíncrono para liberar threads do ASP.NET
            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            int codigo = pCodigo.Value == DBNull.Value ? 0 : Convert.ToInt32(pCodigo.Value);
            string mensagem = pMensagem.Value == DBNull.Value ? string.Empty : pMensagem.Value.ToString() ?? string.Empty;

            return new ProcResult { Codigo = codigo, Mensagem = mensagem };
        }

        // Sincronous wrapper para compatibilidade com chamadas existentes
        public static ProcResult FinalizarOrcamento(string connectionString, int orcamentoId)
        {
            return FinalizarOrcamentoAsync(connectionString, orcamentoId).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Convenience method that reads the connection string from environment variable `BOOTCAMP_MATERA_CONN`.
        /// </summary>
        public static async Task<ProcResult> FinalizarOrcamentoFromEnvAsync(int orcamentoId)
        {
            var cs = Environment.GetEnvironmentVariable("BOOTCAMP_MATERA_CONN");
            if (cs == null)
            {
                throw new InvalidOperationException("Variável de ambiente BOOTCAMP_MATERA_CONN não encontrada");
            }
            return await FinalizarOrcamentoAsync(cs, orcamentoId);
        }

        // Sincronous wrapper para compatibilidade
        public static ProcResult FinalizarOrcamentoFromEnv(int orcamentoId)
        {
            return FinalizarOrcamentoFromEnvAsync(orcamentoId).GetAwaiter().GetResult();
        }
    }
}