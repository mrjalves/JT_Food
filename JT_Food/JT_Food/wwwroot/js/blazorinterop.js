// Objeto para armazenar as instâncias dos gráficos criados
window.blazorCharts = {};

// Função para criar ou atualizar um gráfico com estilo moderno
window.blazorCharts.createChart = (chartId, type, labels, datasets, title) => {
    const ctx = document.getElementById(chartId);
    if (!ctx) {
        console.error(`Elemento do gráfico com id '${chartId}' não encontrado.`);
        return;
    }

    // Paleta de cores moderna e suave
    const modernColors = [
        '#4e73df', '#1cc88a', '#36b9cc', '#f6c23e', '#e74a3b',
        '#858796', '#5a5c69', '#fd7e14', '#6610f2', '#e83e8c'
    ];

    // Adiciona as cores aos datasets se não tiverem sido definidas
    datasets.forEach((ds, index) => {
        ds.backgroundColor = ds.backgroundColor || modernColors;
        ds.borderColor = ds.borderColor || '#ffffff'; // Borda branca para separar fatias
        ds.borderWidth = type === 'pie' || type === 'doughnut' ? 2 : 1;
        ds.borderRadius = type === 'bar' ? 5 : 0; // Bordas arredondadas para gráficos de barra
    });

    // Opções de configuração padrão para todos os gráficos
    const options = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                position: 'bottom', // Legenda na parte inferior
                labels: {
                    font: {
                        family: "'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif"
                    }
                }
            },
            title: {
                display: true,
                text: title,
                font: {
                    size: 18,
                    family: "'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif",
                    weight: '600'
                },
                padding: {
                    top: 10,
                    bottom: 20
                }
            },
            // Configuração do plugin datalabels
            datalabels: {
                // Formata os valores para moeda (R$)
                formatter: (value, context) => {
                    return new Intl.NumberFormat('pt-BR', {
                        style: 'currency',
                        currency: 'BRL'
                    }).format(value);
                },
                color: '#fff',
                font: {
                    weight: 'bold'
                },
                // Esconde labels muito pequenos para não poluir o gráfico
                display: function (context) {
                    var dataset = context.dataset;
                    var value = dataset.data[context.dataIndex];
                    return value > 0; // Ou um percentual do total
                }
            }
        }
    };

    // Opções específicas para gráficos que TÊM eixos (barra, linha, etc.)
    if (type === 'bar' || type === 'line') {
        options.scales = {
            x: {
                grid: {
                    display: false // Remove linhas de grade verticais
                },
                ticks: {
                    font: {
                        family: "'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif"
                    }
                }
            },
            y: {
                grid: {
                    color: '#e3e6f0', // Linha de grade suave
                    borderDash: [2, 4] // Linha tracejada
                },
                ticks: {
                    // Formata os ticks do eixo Y como moeda
                    callback: function (value, index, values) {
                        return new Intl.NumberFormat('pt-BR', {
                            style: 'currency',
                            currency: 'BRL',
                            notation: 'compact' // Ex: R$ 1,5 mil
                        }).format(value);
                    },
                    font: {
                        family: "'Segoe UI', 'Roboto', 'Helvetica Neue', Arial, sans-serif"
                    }
                }
            }
        };
        // Para gráficos de barra, a legenda é geralmente redundante
        options.plugins.legend.display = datasets.length > 1;
    }

    // Se já existe um gráfico com este ID, destrói antes de criar um novo
    if (window.blazorCharts[chartId]) {
        window.blazorCharts[chartId].destroy();
    }

    // Registra o plugin globalmente (necessário apenas uma vez, mas seguro de chamar aqui)
    Chart.register(ChartDataLabels);

    // Cria a nova instância do gráfico
    window.blazorCharts[chartId] = new Chart(ctx, {
        type: type,
        data: {
            labels: labels,
            datasets: datasets
        },
        options: options
    });
};

// Atalhos globais para filtros e pesquisa
window.JTHotkeys = (function () {
    let handler = null;
    let filterEnabled = false;
    let searchEnabled = false;

    function isTypingContext() {
        const ae = document.activeElement;
        if (!ae) return false;
        const tag = ae.tagName;
        const isFormField = tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT';
        return isFormField || ae.isContentEditable === true;
    }

    function matchesFilterHotkey(e) {
        if (e.key === 'F3') return true;
        if (e.altKey && (e.key === 'f' || e.key === 'F')) return true;
        if (e.ctrlKey && e.shiftKey && (e.key === 'f' || e.key === 'F')) return true;
        return false;
    }

    function matchesSearchHotkey(e) {
        // Ctrl+K e "/"
        if (e.ctrlKey && (e.key === 'k' || e.key === 'K')) return true;
        if (!e.ctrlKey && !e.altKey && !e.metaKey && e.key === '/') return true;
        return false;
    }

    function tryFocus(el) {
        if (!el) return false;
        try {
            el.focus();
            el.select?.();
            return true;
        } catch {
            return false;
        }
    }

    function isVisible(el) {
        if (!el) return false;
        const rects = el.getClientRects();
        return !!(el.offsetWidth || el.offsetHeight || rects.length);
    }

    function focusSearchInput() {
        // Tenta achar o input
        const inputSel = 'input[data-role="search-filter"], input#searchFilterInput, .search-container .search-input, input[type="search"]';
        let input = document.querySelector(inputSel);

        // Se não houver input ou não estiver visível, clica no ícone primeiro
        if (!input || !isVisible(input)) {
            const icon = document.querySelector('.search-container i.bi-search.action-icon');
            if (icon) {
                icon.dispatchEvent(new MouseEvent('click', { bubbles: true }));
                setTimeout(() => {
                    input = document.querySelector(inputSel);
                    tryFocus(input);
                }, 50);
                return true;
            }
        }

        // Se já estiver visível, apenas focar
        return tryFocus(input);
    }

    return {
        registerFilterHotkeys: function (dotnetRef) {
            if (handler) {
                document.removeEventListener('keydown', handler, true);
                handler = null;
            }

            handler = function (e) {
                if (isTypingContext()) return;

                if (matchesSearchHotkey(e) && searchEnabled) {
                    e.preventDefault();
                    try {
                        dotnetRef.invokeMethodAsync('HandleSearchHotkey');
                    } catch (err) {
                        console.error('Falha ao invocar HandleSearchHotkey:', err);
                    }
                    return;
                }

                if (matchesFilterHotkey(e) && filterEnabled) {
                    e.preventDefault();
                    try {
                        dotnetRef.invokeMethodAsync('HandleFilterHotkey');
                    } catch (err) {
                        console.error('Falha ao invocar HandleFilterHotkey:', err);
                    }
                }
            };

            document.addEventListener('keydown', handler, true);
        },

        unregisterFilterHotkeys: function () {
            if (handler) {
                document.removeEventListener('keydown', handler, true);
                handler = null;
            }
        },

        enableFilterHotkeys: function () { filterEnabled = true; },
        disableFilterHotkeys: function () { filterEnabled = false; },

        enableSearchHotkeys: function () { searchEnabled = true; },
        disableSearchHotkeys: function () { searchEnabled = false; },

        focusSearchInput: function () { return focusSearchInput(); }
    };
})();

window.BlazorInterop = window.BlazorInterop || {};

window.BlazorInterop.goBack = function (fallbackUrl) {
    try {
        if (history.length > 1 && document.referrer) {
            history.back();
        } else if (fallbackUrl) {
            window.location.href = fallbackUrl;
        }
    } catch {
        if (fallbackUrl) window.location.href = fallbackUrl;
    }
};

// Namespace de atalhos do app
window.JTHotkeys = window.JTHotkeys || {
    _novoHandler: null
};

// Habilita atalhos para o botão "Novo" (Alt+N). Evita Ctrl+N (atalho do navegador).
window.JTHotkeys.enableNewHotkeys = function (dotNetRef) {
    const handler = (e) => {
        const active = document.activeElement;
        const isTyping =
            active && (active.tagName === 'INPUT' || active.tagName === 'TEXTAREA' || active.isContentEditable);

        const key = (e.key || '').toLowerCase();
        const isAltN = e.altKey && key === 'n';

        if (!isTyping && isAltN) {
            e.preventDefault();
            try {
                dotNetRef.invokeMethodAsync('OnNovoHotkey');
            } catch { /* no-op */ }
        }
    };

    if (window.JTHotkeys._novoHandler) {
        window.removeEventListener('keydown', window.JTHotkeys._novoHandler, true);
    }
    window.JTHotkeys._novoHandler = handler;
    window.addEventListener('keydown', handler, true);
};

window.JTHotkeys.disableNewHotkeys = function () {
    if (window.JTHotkeys._novoHandler) {
        window.removeEventListener('keydown', window.JTHotkeys._novoHandler, true);
        window.JTHotkeys._novoHandler = null;
    }
};