using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TesteCode.TesteCode.API.DTO
{
    public class CriarOrcamentoDto
    {
        [Required(ErrorMessage = "O clienteId é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "O clienteId deve ser um ID válido.")]
        public int ClienteId { get; set; }

        [Required(ErrorMessage = "O veiculoId é obrigatório.")]
        [Range(1, int.MaxValue, ErrorMessage = "O veiculoId deve ser um ID válido.")]
        public int VeiculoId { get; set; }

        [Required(ErrorMessage = "O orçamento deve conter pelo menos um item.")]
        [MinLength(1, ErrorMessage = "O orçamento deve ter pelo menos 1 item.")]
        public List<CriarOrcamentoItemDto> Itens { get; set; } = new();
    }

    public class CriarOrcamentoItemDto
    {
        [Required(ErrorMessage = "A descrição do item é obrigatória.")]
        [StringLength(255, ErrorMessage = "A descrição não pode exceder 255 caracteres.")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "A quantidade do item é obrigatória.")]
        [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero.")]
        public int Quantidade { get; set; }

        [Required(ErrorMessage = "O valor unitário é obrigatório.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "O valor unitário deve ser maior que zero.")]
        public decimal ValorUnitario { get; set; }
    }
}