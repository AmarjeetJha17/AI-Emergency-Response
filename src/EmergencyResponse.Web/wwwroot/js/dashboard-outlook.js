/* Forecast presentation is separate from operational incident state. */
(function (root) {
    'use strict';
    function describeOutlook(volume, hotspots) {
        const validHotspots = Array.isArray(hotspots) &&
            hotspots.every(p => p && ['Increasing', 'Decreasing', 'Stable'].includes(p.trend));
        const hotspotCount = validHotspots ? String(hotspots.filter(p => p.trend === 'Increasing').length) : 'Unavailable';
        if (!volume || volume.isAvailable !== true || !Array.isArray(volume.points) ||
            volume.points.length === 0 || volume.points.some(p =>
                !p || typeof p.forecast !== 'number' || !Number.isFinite(p.forecast) || p.forecast < 0)) {
            return { available: false, volume: 'Unavailable', hotspots: hotspotCount,
                shortage: 'Not assessed', status: 'Incident-volume forecast is unavailable or incomplete. ' +
                    (validHotspots ? 'Hotspot trends reflect recent report activity, not a model forecast. ' : 'Hotspot data is unavailable. ') +
                    'No operational conclusion has been made.' };
        }
        const generated = new Date(volume.generatedAtUtc);
        const total = volume.points.reduce((sum, p) => sum + p.forecast, 0);
        if (!Number.isFinite(total) || !Number.isFinite(generated.getTime()) || generated.getUTCFullYear() < 2000) {
            return { available: false, volume: 'Unavailable', hotspots: hotspotCount,
                shortage: 'Not assessed', status: 'Forecast data is incomplete. Generation time has not been verified.' };
        }
        return { available: validHotspots, volume: String(Math.round(total)),
            hotspots: hotspotCount,
            shortage: 'Not assessed', status: 'Advisory forecast generated ' + generated.toLocaleString() +
                (validHotspots ? '. Hotspot trends are based on recent report activity.' : '. Hotspot data is unavailable.') +
                ' Resource demand was not requested; confirm current conditions before acting.' };
    }
    if (typeof module !== 'undefined' && module.exports) module.exports = { describeOutlook };
    if (!root.document) return;
    const document = root.document;
    root.focusSelectedIncident = function () {
        const id = document.getElementById('emergencyDashboard')?.dataset.selectedIncidentId;
        const grid = document.getElementById('incidentGrid')?.ej2_instances?.[0];
        if (!id || !grid || !Array.isArray(grid.currentViewData)) return;
        const index = grid.currentViewData.findIndex(item => String(item.Id).toLowerCase() === id.toLowerCase());
        if (index < 0) return;
        grid.selectRow(index);
        grid.getRowByIndex(index)?.scrollIntoView({
            behavior: root.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'nearest'
        });
    };
    document.addEventListener('DOMContentLoaded', function () {
        root.setTimeout(root.focusSelectedIncident, 300);
        root.setTimeout(root.focusSelectedIncident, 800);
        const status = document.getElementById('predictiveOutlookStatus');
        const button = document.getElementById('refreshPredictiveOutlook');
        if (!status || !button) return;
        function show(result) {
            document.getElementById('forecastIncidentVolume').textContent = result.volume;
            document.getElementById('forecastHotspotCount').textContent = result.hotspots;
            document.getElementById('forecastShortageCount').textContent = result.shortage;
            status.textContent = result.status;
            status.dataset.state = result.available ? 'available' : 'failed';
        }
        async function load() {
            if (button.disabled) return;
            button.disabled = true;
            status.setAttribute('aria-busy', 'true');
            status.textContent = 'Loading advisory forecasts...';
            for (const id of ['forecastIncidentVolume', 'forecastHotspotCount', 'forecastShortageCount']) {
                document.getElementById(id).textContent = 'Loading...';
            }
            try {
                const responses = await Promise.all([
                    root.fetch('/Dashboard/IncidentVolume', { credentials: 'same-origin', cache: 'no-store' }),
                    root.fetch('/Dashboard/Hotspots', { credentials: 'same-origin', cache: 'no-store' })
                ]);
                if (responses.some(r => !r.ok)) throw new Error('Forecast request failed');
                const payloads = await Promise.all(responses.map(r => r.json()));
                show(describeOutlook(payloads[0], payloads[1]));
            } catch (_) {
                show({ available: false, volume: 'Unavailable', hotspots: 'Unavailable', shortage: 'Not assessed',
                    status: 'Forecast request failed. Previous values have been cleared; core incident operations are unchanged.' });
            } finally {
                button.disabled = false;
                status.setAttribute('aria-busy', 'false');
            }
        }
        button.addEventListener('click', load);
        load();
    });
}(typeof window === 'undefined' ? globalThis : window));
