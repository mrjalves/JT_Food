using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using JT_Food.Shared.Domain.Entities;
using JT_Food.Shared.Services.Interfaces;

namespace JT_Food.Shared.Services.Implementations
{
    public class EscPosPrintService : IImpressaoRedeService
    {
        private readonly List<ImpressoraRede> _impressoras = new();
        private readonly List<(Pedido Pedido, string Setor, string Motivo)> _filaFalhas = new();

        public EscPosPrintService()
        {
            InicializarImpressorasPadrao();
        }

        private void InicializarImpressorasPadrao()
        {
            if (_impressoras.Any()) return;

            _impressoras.AddRange(new[]
            {
                new ImpressoraRede
                {
                    Id = 1,
                    NomeSetor = "Cozinha Central (Pratos & Porções)",
                    EnderecoIp = "192.168.1.201",
                    PortaTcp = 9100,
                    LarguraColunas = 48,
                    Tipo = "Cozinha",
                    Ativa = true
                },
                new ImpressoraRede
                {
                    Id = 2,
                    NomeSetor = "Bar & Bebidas",
                    EnderecoIp = "192.168.1.202",
                    PortaTcp = 9100,
                    LarguraColunas = 48,
                    Tipo = "Bar",
                    Ativa = true
                },
                new ImpressoraRede
                {
                    Id = 3,
                    NomeSetor = "Chapa & Hambúrgueres",
                    EnderecoIp = "192.168.1.203",
                    PortaTcp = 9100,
                    LarguraColunas = 48,
                    Tipo = "Chapa",
                    Ativa = true
                },
                new ImpressoraRede
                {
                    Id = 4,
                    NomeSetor = "Caixa / Fechamento",
                    EnderecoIp = "192.168.1.200",
                    PortaTcp = 9100,
                    LarguraColunas = 48,
                    Tipo = "Caixa",
                    Ativa = true
                }
            });
        }

        public Task<List<ImpressoraRede>> ObterImpressorasAsync()
        {
            return Task.FromResult(_impressoras.ToList());
        }

        public Task<bool> SalvarImpressoraAsync(ImpressoraRede impressora)
        {
            if (impressora.Id == 0)
            {
                impressora.Id = _impressoras.Any() ? _impressoras.Max(i => i.Id) + 1 : 1;
                _impressoras.Add(impressora);
            }
            else
            {
                var idx = _impressoras.FindIndex(i => i.Id == impressora.Id);
                if (idx >= 0) _impressoras[idx] = impressora;
                else _impressoras.Add(impressora);
            }
            return Task.FromResult(true);
        }

        public Task<bool> ExcluirImpressoraAsync(int id)
        {
            var removido = _impressoras.RemoveAll(i => i.Id == id) > 0;
            return Task.FromResult(removido);
        }

        public async Task<(bool Sucesso, string Mensagem)> TestarConexaoAsync(ImpressoraRede impressora)
        {
            if (OperatingSystem.IsBrowser())
            {
                impressora.UltimaImpressaoOk = true;
                impressora.UltimoErro = null;
                return (true, $"Simulação no Navegador: Impressora '{impressora.NomeSetor}' ({impressora.EnderecoIp}:{impressora.PortaTcp}) ativa.");
            }

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(impressora.EnderecoIp, impressora.PortaTcp);
                if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
                {
                    impressora.UltimaImpressaoOk = false;
                    impressora.UltimoErro = "Tempo limite esgotado (timeout 2s).";
                    return (false, "Impressora não respondeu no IP/Porta informados (Timeout).");
                }

                impressora.UltimaImpressaoOk = true;
                impressora.UltimoErro = null;
                return (true, $"Conexão TCP com '{impressora.NomeSetor}' realizada com sucesso!");
            }
            catch (Exception ex)
            {
                impressora.UltimaImpressaoOk = false;
                impressora.UltimoErro = ex.Message;
                return (false, $"Falha de conexão com a impressora: {ex.Message}");
            }
        }

        public async Task<List<(string Setor, bool Sucesso, string Mensagem)>> RoteamentoEImpressaoPedidoAsync(Pedido pedido)
        {
            var resultados = new List<(string Setor, bool Sucesso, string Mensagem)>();

            if (pedido == null || !pedido.Itens.Any())
                return resultados;

            // Agrupa itens por impressora destino
            var gruposPorImpressora = pedido.Itens.GroupBy(i => i.ImpressoraDestinoId ?? 1).ToList();

            foreach (var grupo in gruposPorImpressora)
            {
                var imp = _impressoras.FirstOrDefault(x => x.Id == grupo.Key)
                         ?? _impressoras.FirstOrDefault(x => x.Tipo == "Cozinha")
                         ?? new ImpressoraRede { NomeSetor = "Impressora Padrão", EnderecoIp = "127.0.0.1" };

                var itensDoSetor = grupo.ToList();
                byte[] bytesEscPos = GerarComandosEscPosTicket(pedido, itensDoSetor, imp.NomeSetor, imp.LarguraColunas);

                var envio = await EnviarBytesTcpAsync(imp, bytesEscPos);
                resultados.Add((imp.NomeSetor, envio.Sucesso, envio.Mensagem));

                if (!envio.Sucesso)
                {
                    _filaFalhas.Add((pedido, imp.NomeSetor, envio.Mensagem));
                }
            }

            return resultados;
        }

        public async Task<(bool Sucesso, string Mensagem)> ImprimirContaAsync(Comanda comanda)
        {
            var impCaixa = _impressoras.FirstOrDefault(i => i.Tipo == "Caixa")
                        ?? _impressoras.FirstOrDefault()
                        ?? new ImpressoraRede { NomeSetor = "Caixa", EnderecoIp = "127.0.0.1" };

            byte[] bytesEscPos = GerarComandosEscPosConta(comanda, impCaixa.LarguraColunas);
            return await EnviarBytesTcpAsync(impCaixa, bytesEscPos);
        }

        private async Task<(bool Sucesso, string Mensagem)> EnviarBytesTcpAsync(ImpressoraRede impressora, byte[] dados)
        {
            if (!impressora.Ativa)
            {
                return (true, $"Impressão em '{impressora.NomeSetor}' simulada (Impressora pausada).");
            }

            if (OperatingSystem.IsBrowser())
            {
                impressora.UltimaImpressaoOk = true;
                impressora.UltimoErro = null;
                return (true, $"Enviado com sucesso para '{impressora.NomeSetor}' (Modo Web/Navegador).");
            }

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(impressora.EnderecoIp, impressora.PortaTcp);
                if (await Task.WhenAny(connectTask, Task.Delay(2000)) != connectTask)
                {
                    impressora.UltimaImpressaoOk = false;
                    impressora.UltimoErro = "Timeout ao conectar na impressora de rede.";
                    return (false, $"Timeout de rede na impressora '{impressora.NomeSetor}' ({impressora.EnderecoIp}:{impressora.PortaTcp}).");
                }

                using var stream = client.GetStream();
                await stream.WriteAsync(dados, 0, dados.Length);
                await stream.FlushAsync();

                impressora.UltimaImpressaoOk = true;
                impressora.UltimoErro = null;
                return (true, $"Enviado com sucesso para '{impressora.NomeSetor}'.");
            }
            catch (Exception ex)
            {
                impressora.UltimaImpressaoOk = false;
                impressora.UltimoErro = ex.Message;
                // Retorna falha detalhada para a UI exibir alerta instantâneo
                return (false, $"Falha ao imprimir em '{impressora.NomeSetor}': {ex.Message}");
            }
        }

        private byte[] GerarComandosEscPosTicket(Pedido pedido, List<ItemPedido> itens, string nomeSetor, int colunas)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            // ESC @ - Inicializar impressora
            bw.Write(new byte[] { 0x1B, 0x40 });

            // ESC a 1 - Alinhamento Centralizado
            bw.Write(new byte[] { 0x1B, 0x61, 0x01 });

            // GS ! 0x11 - Fonte Dobrada (Altura e Largura)
            bw.Write(new byte[] { 0x1D, 0x21, 0x11 });
            bw.Write(Encoding.UTF8.GetBytes($"[ {nomeSetor.ToUpper()} ]\n"));

            // GS ! 0x00 - Fonte Normal
            bw.Write(new byte[] { 0x1D, 0x21, 0x00 });
            bw.Write(Encoding.UTF8.GetBytes(new string('-', colunas) + "\n"));

            // Dados da Mesa e Comanda
            string mesaOuComanda = !string.IsNullOrEmpty(pedido.MesaId) ? $"MESA: {pedido.MesaId}" : "SEM MESA";
            bw.Write(new byte[] { 0x1B, 0x45, 0x01 }); // Negrito
            bw.Write(Encoding.UTF8.GetBytes($"{mesaOuComanda}  |  COMANDA #{pedido.ComandaId}\n"));
            bw.Write(new byte[] { 0x1B, 0x45, 0x00 }); // Negrito off

            bw.Write(Encoding.UTF8.GetBytes($"Garçom: {pedido.NomeGarcom}  |  Hora: {pedido.DataHora:dd/MM HH:mm:ss}\n"));
            bw.Write(Encoding.UTF8.GetBytes(new string('-', colunas) + "\n"));

            // ESC a 0 - Alinhamento à Esquerda
            bw.Write(new byte[] { 0x1B, 0x61, 0x00 });

            foreach (var item in itens)
            {
                bw.Write(new byte[] { 0x1B, 0x45, 0x01 }); // Negrito
                bw.Write(Encoding.UTF8.GetBytes($"{item.Quantidade}x {item.NomeProduto}\n"));
                bw.Write(new byte[] { 0x1B, 0x45, 0x00 }); // Negrito off

                if (!string.IsNullOrWhiteSpace(item.Observacao))
                {
                    bw.Write(Encoding.UTF8.GetBytes($"   >> OBS: {item.Observacao}\n"));
                }
                bw.Write(Encoding.UTF8.GetBytes("\n"));
            }

            if (!string.IsNullOrWhiteSpace(pedido.ObservacaoGeral))
            {
                bw.Write(Encoding.UTF8.GetBytes(new string('-', colunas) + "\n"));
                bw.Write(Encoding.UTF8.GetBytes($"OBS GERAL: {pedido.ObservacaoGeral}\n"));
            }

            bw.Write(Encoding.UTF8.GetBytes(new string('=', colunas) + "\n\n\n"));

            // ESC d 3 - Avançar 3 linhas
            bw.Write(new byte[] { 0x1B, 0x64, 0x03 });
            // GS V 66 0 - Corte total de papel
            bw.Write(new byte[] { 0x1D, 0x56, 0x42, 0x00 });

            return ms.ToArray();
        }

        private byte[] GerarComandosEscPosConta(Comanda comanda, int colunas)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write(new byte[] { 0x1B, 0x40 }); // Init
            bw.Write(new byte[] { 0x1B, 0x61, 0x01 }); // Centralizado
            bw.Write(new byte[] { 0x1B, 0x45, 0x01 }); // Bold
            bw.Write(Encoding.UTF8.GetBytes("JT FOOD RESTAURANTE & BAR\n"));
            bw.Write(new byte[] { 0x1B, 0x45, 0x00 });
            bw.Write(Encoding.UTF8.GetBytes("--- CONFERÊNCIA DE CONTA ---\n"));
            bw.Write(Encoding.UTF8.GetBytes($"{comanda.DisplayIdentificacao}\n"));
            if (!string.IsNullOrEmpty(comanda.MesaId))
            {
                bw.Write(Encoding.UTF8.GetBytes($"Mesa: {comanda.MesaId}\n"));
            }
            bw.Write(Encoding.UTF8.GetBytes($"Abertura: {comanda.DataAbertura:dd/MM/yyyy HH:mm}\n"));
            bw.Write(Encoding.UTF8.GetBytes(new string('-', colunas) + "\n"));

            bw.Write(new byte[] { 0x1B, 0x61, 0x00 }); // Esquerda

            foreach (var item in comanda.Itens)
            {
                string linha = $"{item.Quantidade}x {item.NomeProduto}".PadRight(colunas - 12);
                string preco = $"R$ {item.TotalItem:F2}".PadLeft(12);
                bw.Write(Encoding.UTF8.GetBytes(linha + preco + "\n"));
            }

            bw.Write(Encoding.UTF8.GetBytes(new string('-', colunas) + "\n"));

            string strSubtotal = $"Subtotal: R$ {comanda.Subtotal:F2}".PadLeft(colunas);
            bw.Write(Encoding.UTF8.GetBytes(strSubtotal + "\n"));

            if (comanda.PossuiCouvert)
            {
                string strCouvert = $"Couvert ({comanda.QuantidadePessoas}x): R$ {(comanda.ValorCouvert * comanda.QuantidadePessoas):F2}".PadLeft(colunas);
                bw.Write(Encoding.UTF8.GetBytes(strCouvert + "\n"));
            }

            if (comanda.TaxaServico > 0)
            {
                string strTaxa = $"Serviço (10%): R$ {comanda.TaxaServico:F2}".PadLeft(colunas);
                bw.Write(Encoding.UTF8.GetBytes(strTaxa + "\n"));
            }

            if (comanda.Desconto > 0)
            {
                string strDesc = $"Desconto: -R$ {comanda.Desconto:F2}".PadLeft(colunas);
                bw.Write(Encoding.UTF8.GetBytes(strDesc + "\n"));
            }

            bw.Write(Encoding.UTF8.GetBytes(new string('=', colunas) + "\n"));
            bw.Write(new byte[] { 0x1D, 0x21, 0x11 }); // Fonte grande
            string strTotal = $"TOTAL: R$ {comanda.Total:F2}".PadLeft(colunas / 2);
            bw.Write(Encoding.UTF8.GetBytes(strTotal + "\n"));
            bw.Write(new byte[] { 0x1D, 0x21, 0x00 });

            if (comanda.QuantidadePessoas > 1)
            {
                double porPessoa = comanda.Total / comanda.QuantidadePessoas;
                bw.Write(Encoding.UTF8.GetBytes($"\nDivisão por pessoa ({comanda.QuantidadePessoas} pagantes): R$ {porPessoa:F2}\n"));
            }

            bw.Write(new byte[] { 0x1B, 0x61, 0x01 });
            bw.Write(Encoding.UTF8.GetBytes("\nObrigado pela preferência! Volte sempre.\n\n\n"));

            bw.Write(new byte[] { 0x1B, 0x64, 0x03 });
            bw.Write(new byte[] { 0x1D, 0x56, 0x42, 0x00 }); // Corte

            return ms.ToArray();
        }
    }
}
