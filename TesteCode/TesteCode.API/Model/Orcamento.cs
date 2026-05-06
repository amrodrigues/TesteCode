using System;
using System.Collections.Generic;
using System.Text;

namespace TesteCode.TesteCode.API.Model
{
    public class Orcamento
    {
        public int Id { get; set; }
        public int ClienteId { get; set; }
        public int VeiculoId { get; set; }
        public string Status { get; set; } = "Aberto";
        public decimal ValorTotal { get; set; }
        public DateTime DataCriacao { get; set; } = DateTime.Now;
        public DateTime? DataFinalizacao { get; set; }
        public List<OrcamentoItem> Itens { get; set; } = new();
    }
}
