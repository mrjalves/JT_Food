namespace JT_Food.Shared.Domain.Entities
{
    public class ItemPedido
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public int ProdutoId { get; set; }
        public string NomeProduto { get; set; } = string.Empty;
        public double Quantidade { get; set; } = 1.0;
        public double PrecoUnitario { get; set; } = 0.0;
        public double TotalItem => Quantidade * PrecoUnitario;
        public string? Observacao { get; set; }
        public string EstadoKDS { get; set; } = "Confirmado";
        public bool Impresso { get; set; } = false;
        public int? ImpressoraDestinoId { get; set; }
        public string? NomeSetorImpressao { get; set; }
    }
}
