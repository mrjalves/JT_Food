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
