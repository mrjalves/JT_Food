namespace JT_Food.Shared.Domain.Entities
{
    public class ImpressoraRede
    {
        public int Id { get; set; }
        public string NomeSetor { get; set; } = "Cozinha";
        public string EnderecoIp { get; set; } = "192.168.1.200";
        public int PortaTcp { get; set; } = 9100;
        public int LarguraColunas { get; set; } = 48; // 48 para 80mm, 32 para 58mm
        public string Tipo { get; set; } = "Cozinha"; // Cozinha, Bar, Chapa, Caixa
        public bool Ativa { get; set; } = true;
        public bool UltimaImpressaoOk { get; set; } = true;
        public string? UltimoErro { get; set; }
    }
}
