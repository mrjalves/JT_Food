// Objeto para armazenar as instâncias dos gráficos criados
window.blazorCharts = {};

// Função para criar ou atualizar um gráfico com estilo moderno
window.blazorCharts.createChart = (chartId, type, labels, datasets, title) => {
    const ctx = document.getElementById(chartId);
    if (!ctx) {
        console.error(`Elemento do gráfico com id '${chartId}' não encontrado.`);
        return;
    }

    const modernColors = [
        '#4e73df', '#1cc88a', '#36b9cc', '#f6c23e', '#e74a3b',
        '#858796', '#5a5c69', '#fd7e14', '#6610f2', '#e83e8c'
    ];

    datasets.forEach((ds, index) => {
        ds.backgroundColor = ds.backgroundColor || modernColors;
        ds.borderColor = ds.borderColor || '#ffffff';
        ds.borderWidth = type === 'pie' || type === 'doughnut' ? 2 : 1;
        ds.borderRadius = type === 'bar' ? 5 : 0;
    });

    const options = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                position: 'bottom',
                labels: {
                    font: {
                        family: "'Inter', 'Segoe UI', Arial, sans-serif"
                    }
                }
            },
            title: {
                display: true,
                text: title,
                font: {
                    size: 16,
                    family: "'Outfit', 'Segoe UI', Arial, sans-serif",
                    weight: '600'
                },
                padding: { top: 10, bottom: 20 }
            }
        }
    };

    if (type === 'bar' || type === 'line') {
        options.scales = {
            x: { grid: { display: false } },
            y: {
                grid: { color: '#e3e6f0', borderDash: [2, 4] },
                ticks: {
                    callback: function (value) {
                        return 'R$ ' + value.toLocaleString('pt-BR');
                    }
                }
            }
        };
        options.plugins.legend.display = datasets.length > 1;
    }

    if (window.blazorCharts[chartId]) {
        window.blazorCharts[chartId].destroy();
    }

    window.blazorCharts[chartId] = new Chart(ctx, {
        type: type,
        data: { labels: labels, datasets: datasets },
        options: options
    });
};

window.JTHotkeys = window.JTHotkeys || { _novoHandler: null };

window.JTHotkeys.enableNewHotkeys = function (dotNetRef) {
    const handler = (e) => {
        const active = document.activeElement;
        const isTyping = active && (active.tagName === 'INPUT' || active.tagName === 'TEXTAREA' || active.isContentEditable);
        const isAltN = e.altKey && (e.key || '').toLowerCase() === 'n';

        if (!isTyping && isAltN) {
            e.preventDefault();
            try { dotNetRef.invokeMethodAsync('OnNovoHotkey'); } catch { }
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
    let draggingEl = null;
    let canvasEl = null;
    let elemId = 0;

    function onPointerMove(e) {
        if (!draggingEl || !canvasEl) return;
        e.preventDefault();

        const rect = canvasEl.getBoundingClientRect();
        const clientX = e.clientX || (e.touches && e.touches[0].clientX);
        const clientY = e.clientY || (e.touches && e.touches[0].clientY);

        if (!clientX || !clientY) return;

        let x = ((clientX - rect.left) / rect.width) * 100;
        let y = ((clientY - rect.top) / rect.height) * 100;

        x = Math.max(0, Math.min(92, x));
        y = Math.max(0, Math.min(92, y));

        draggingEl.style.left = x.toFixed(2) + '%';
        draggingEl.style.top = y.toFixed(2) + '%';
    }

    function onPointerUp(e) {
        if (!draggingEl || !canvasEl) return;

        const leftPercent = parseFloat(draggingEl.style.left) || 0;
        const topPercent = parseFloat(draggingEl.style.top) || 0;

        if (dotNetRef && elemId) {
            try {
                dotNetRef.invokeMethodAsync('OnElementoArrastado', elemId, leftPercent, topPercent);
            } catch (err) {
                console.error('Erro ao notificar Blazor sobre drag-and-drop:', err);
            }
        }

        draggingEl.classList.remove('arrastando');
        draggingEl = null;
        elemId = 0;

        window.removeEventListener('pointermove', onPointerMove);
        window.removeEventListener('pointerup', onPointerUp);
        window.removeEventListener('touchmove', onPointerMove);
        window.removeEventListener('touchend', onPointerUp);
    }

    return {
        iniciarEditor: function (refNet, canvasContainerId) {
            dotNetRef = refNet;
            canvasEl = document.getElementById(canvasContainerId);
        },

        iniciarArrasto: function (elementoId, domElement) {
            if (!canvasEl) return;
            elemId = elementoId;
            draggingEl = domElement;
            draggingEl.classList.add('arrastando');

            window.addEventListener('pointermove', onPointerMove, { passive: false });
            window.addEventListener('pointerup', onPointerUp);
            window.addEventListener('touchmove', onPointerMove, { passive: false });
            window.addEventListener('touchend', onPointerUp);
        }
    };
})();
