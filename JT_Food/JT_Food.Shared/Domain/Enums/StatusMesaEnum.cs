namespace JT_Food.Shared.Domain.Enums
{
    public enum StatusMesaEnum
    {
        Livre = 1,
        Ocupada = 2,
        AlertaInatividade = 3,
        AguardandoPagamento = 4,
        Reservada = 5
    }

    public static class StatusMesaConsts
    {
        public const string Livre = "Livre";
        public const string Ocupada = "Ocupada";
        public const string AlertaInatividade = "AlertaInatividade";
        public const string AguardandoPagamento = "AguardandoPagamento";
        public const string Reservada = "Reservada";
    }
}
