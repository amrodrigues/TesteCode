using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TesteCode.TesteCode.API.Model;

namespace TesteCode.TesteCode.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrcamentoController : ControllerBase
    {
        // Caso esteja usando Entity Framework, você injetaria seu DbContext aqui:
        // private readonly OficinaDbContext _context;

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Orcamento))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult CadastrarOrcamento([FromBody] CriarOrcamentoDto dto)
        {
            // 1. O [ApiController] valida automaticamente as anotações do DTO (DataAnnotations).
            // Se o JSON estiver mal formatado ou faltar campos básicos, ele já retorna 400 Bad Request automaticamente.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 2. Validações adicionais de regra de negócio (garantia extra)
            if (dto.Itens == null || !dto.Itens.Any())
            {
                return BadRequest(new { Mensagem = "O orçamento deve possuir pelo menos 1 item." });
            }

            // 3. Mapeia o DTO de entrada para a nossa Entidade de negócio e calcula os totais
            var novoOrcamento = new Orcamento
            {
                ClienteId = dto.ClienteId,
                VeiculoId = dto.VeiculoId,
                Status = "Aberto",
                DataCriacao = DateTime.Now
            };

            decimal valorTotalOrcamento = 0;

            foreach (var itemDto in dto.Itens)
            {
                // Dupla validação de segurança para regras de valores negativos/zerados
                if (itemDto.Quantidade <= 0 || itemDto.ValorUnitario <= 0)
                {
                    return BadRequest(new { Mensagem = $"O item '{itemDto.Descricao}' possui quantidade ou valor unitário inválido." });
                }

                // Calcula o total do item individual
                decimal totalItem = itemDto.Quantidade * itemDto.ValorUnitario;
                valorTotalOrcamento += totalItem;

                novoOrcamento.Itens.Add(new OrcamentoItem
                {
                    Descricao = itemDto.Descricao,
                    Quantidade = itemDto.Quantidade,
                    ValorUnitario = itemDto.ValorUnitario,
                    ValorTotal = totalItem
                });
            }

            // Atribui o total calculado pela API ao orçamento pai
            novoOrcamento.ValorTotal = valorTotalOrcamento;

            try
            {
                // 4. Salva no banco de dados (Exemplo usando EF Core)
                // _context.Orcamento.Add(novoOrcamento);
                // _context.SaveChanges();

                // Simulando que o ID foi gerado pelo banco para o retorno
                novoOrcamento.Id = new Random().Next(1, 1000);

                // Retorna 201 Created apontando para onde o recurso pode ser consultado futuramente
                return CreatedAtAction(nameof(CadastrarOrcamento), new { id = novoOrcamento.Id }, novoOrcamento);
            }
            catch (Exception ex)
            {
                // Trata falhas de banco de dados ou infraestrutura de forma amigável
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Mensagem = "Ocorreu um erro interno ao salvar o orçamento.", Detalhes = ex.Message });
            }
        }
    }
}