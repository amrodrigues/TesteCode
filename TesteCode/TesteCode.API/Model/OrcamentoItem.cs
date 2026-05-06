using System;
using System.Collections.Generic;
using System.Text;

namespace TesteCode.TesteCode.API.Model
{
    public class OrcamentoItem
    {
        public int Id { get; set; }
        public int OrcamentoId { get; set; }
        public string Descricao { get; set; } = string.Empty;
        public int Quantidade { get; set; }
        public decimal ValorUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }
}
