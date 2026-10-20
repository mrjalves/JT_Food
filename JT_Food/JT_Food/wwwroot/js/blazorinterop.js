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

// ==========================================
// Construtor e Mapa Interativo de Salão (Drag & Drop)
// ==========================================
window.JTSalaoLayout = (function () {
    let dotNetRef = null;
    let canvasEl = null;
    let activeElemId = null;
    let draggingEl = null;
    let isDragging = false;
    let startX = 0, startY = 0;
    let startLeft = 0, startTop = 0;
    let hasMoved = false;

    function handlePointerDown(e) {
        // Ignora clique em botões de ação ou alças de rotação para não disparar drag
        if (e.target.closest('.elem-action-btn') || e.target.closest('.alca-rotacao') || e.target.closest('button')) {
            return;
        }

        const targetElem = e.target.closest('.elemento-editor');
        if (!targetElem || !canvasEl) return;

        const idStr = targetElem.getAttribute('data-elem-id');
        if (!idStr) return;

        const id = parseInt(idStr, 10);
        activeElemId = id;
        draggingEl = targetElem;
        isDragging = true;
        hasMoved = false;

        startX = e.clientX;
        startY = e.clientY;

        startLeft = parseFloat(targetElem.style.left) || 0;
        startTop = parseFloat(targetElem.style.top) || 0;

        try {
            targetElem.setPointerCapture(e.pointerId);
        } catch (_) {}

        // Notifica Blazor sobre a seleção imediatamente
        if (dotNetRef) {
            try {
                dotNetRef.invokeMethodAsync('OnElementoSelecionadoJS', id);
            } catch (_) {}
        }
    }

    function handlePointerMove(e) {
        if (!isDragging || !draggingEl || !canvasEl) return;

        const dx = e.clientX - startX;
        const dy = e.clientY - startY;

        if (!hasMoved && (Math.abs(dx) > 3 || Math.abs(dy) > 3)) {
            hasMoved = true;
            draggingEl.classList.add('arrastando');
        }

        if (!hasMoved) return;

        e.preventDefault();

        const canvasRect = canvasEl.getBoundingClientRect();
        if (canvasRect.width === 0 || canvasRect.height === 0) return;

        const dxPercent = (dx / canvasRect.width) * 100;
        const dyPercent = (dy / canvasRect.height) * 100;

        const elemWidth = parseFloat(draggingEl.style.width) || 10;
        const elemHeight = parseFloat(draggingEl.style.height) || 10;

        let newLeft = startLeft + dxPercent;
        let newTop = startTop + dyPercent;

        // Limites da área do salão
        newLeft = Math.max(0, Math.min(100 - elemWidth, newLeft));
        newTop = Math.max(0, Math.min(100 - elemHeight, newTop));

        // Snap sutil para 0.5%
        newLeft = Math.round(newLeft * 2) / 2;
        newTop = Math.round(newTop * 2) / 2;

        draggingEl.style.left = newLeft.toFixed(1) + '%';
        draggingEl.style.top = newTop.toFixed(1) + '%';
    }

    function handlePointerUp(e) {
        if (!isDragging || !draggingEl) return;

        try {
            draggingEl.releasePointerCapture(e.pointerId);
        } catch (_) {}

        draggingEl.classList.remove('arrastando');

        if (hasMoved && dotNetRef && activeElemId) {
            const finalLeft = parseFloat(draggingEl.style.left) || 0;
            const finalTop = parseFloat(draggingEl.style.top) || 0;
            try {
                dotNetRef.invokeMethodAsync('OnElementoArrastado', activeElemId, finalLeft, finalTop);
            } catch (err) {
                console.error('Erro ao notificar Blazor sobre drag-and-drop:', err);
            }
        }

        isDragging = false;
        draggingEl = null;
        activeElemId = null;
        hasMoved = false;
    }

    return {
        iniciarEditor: function (refNet, canvasContainerId) {
            dotNetRef = refNet;
            canvasEl = document.getElementById(canvasContainerId);
            if (!canvasEl) return;

            // Remove ouvintes prévios para evitar duplicação em re-renders
            canvasEl.removeEventListener('pointerdown', handlePointerDown);
            window.removeEventListener('pointermove', handlePointerMove);
            window.removeEventListener('pointerup', handlePointerUp);
            window.removeEventListener('pointercancel', handlePointerUp);

            canvasEl.addEventListener('pointerdown', handlePointerDown);
            window.addEventListener('pointermove', handlePointerMove, { passive: false });
            window.addEventListener('pointerup', handlePointerUp);
            window.addEventListener('pointercancel', handlePointerUp);
        }
    };
})();