/**
 * Emergency Response - Operational Situations Map Layer
 * Phase 12 Part 3: Live Map Situation Overlay, Envelope Buffer & Member Incidents
 */
(function () {
    'use strict';

    const situationLayers = new Map();
    let situationsVisible = true;
    let leafletMap = null;

    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function getLevelColor(level) {
        const lvl = typeof level === 'number' ? level : (
            level === 'Critical' ? 4 :
            level === 'High' ? 3 :
            level === 'Elevated' ? 2 : 1
        );

        switch (lvl) {
            case 4: return '#dc3545'; // Critical - Red
            case 3: return '#fd7e14'; // High - Orange
            case 2: return '#0d6efd'; // Elevated - Blue
            default: return '#6c757d'; // Advisory - Gray
        }
    }

    function getLevelName(level) {
        if (typeof level === 'string') return level;
        switch (level) {
            case 4: return 'Critical';
            case 3: return 'High';
            case 2: return 'Elevated';
            default: return 'Advisory';
        }
    }

    function getLevelBadgeClass(level) {
        const lvl = typeof level === 'number' ? level : (
            level === 'Critical' ? 4 :
            level === 'High' ? 3 :
            level === 'Elevated' ? 2 : 1
        );

        switch (lvl) {
            case 4: return 'bg-danger text-white';
            case 3: return 'bg-warning text-dark';
            case 2: return 'bg-primary text-white';
            default: return 'bg-secondary text-white';
        }
    }

    function renderSituation(situation, map) {
        if (!map || !situation || !situation.centerLatitude || !situation.centerLongitude) {
            return;
        }

        const center = [situation.centerLatitude, situation.centerLongitude];
        const color = getLevelColor(situation.level);

        const layerGroup = L.layerGroup();

        // 1. Awareness Radius Circle Envelope (1.20x buffer, min 500m)
        const circle = L.circle(center, {
            radius: situation.radiusMeters || 500,
            color: color,
            weight: 2,
            fillColor: color,
            fillOpacity: 0.08,
            dashArray: '8 6'
        });

        // 2. Situation Centroid Marker
        const markerIcon = L.divIcon({
            className: 'situation-map-icon',
            html: `<div class="situation-pin" style="background-color: ${color}; border: 2px solid #ffffff; box-shadow: 0 2px 6px rgba(0,0,0,0.35); border-radius: 50%; width: 30px; height: 30px; display: flex; align-items: center; justify-content: center; color: #ffffff; font-size: 14px;">
                     <i class="bi bi-diagram-3-fill"></i>
                   </div>`,
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        });

        const marker = L.marker(center, {
            icon: markerIcon,
            zIndexOffset: 650
        });

        // 3. Member incidents list inside popup
        let incidentsListHtml = '';
        if (situation.incidents && situation.incidents.length > 0) {
            incidentsListHtml = situation.incidents.map(inc => `
                <li class="d-flex justify-content-between align-items-center py-1 border-bottom small">
                    <div>
                        <a href="/Incidents/Details/${inc.incidentId}" class="fw-bold text-decoration-none">
                            ${escapeHtml(inc.incidentNumber)}
                        </a>
                        <span class="text-muted ms-1">${escapeHtml(inc.title || inc.incidentType)}</span>
                    </div>
                    <span class="badge ${inc.severity === 4 || inc.severity === 'Critical' ? 'bg-danger' : 'bg-secondary'}">${escapeHtml(inc.severity)}</span>
                </li>
            `).join('');
        } else {
            incidentsListHtml = '<li class="text-muted small py-1">No member incidents loaded.</li>';
        }

        const popupContent = `
            <div class="situation-map-popup p-1" style="min-width: 250px; max-width: 320px;">
                <div class="d-flex justify-content-between align-items-center mb-1">
                    <strong class="fs-6 text-dark">${escapeHtml(situation.situationNumber)}</strong>
                    <span class="badge ${getLevelBadgeClass(situation.level)}">${escapeHtml(getLevelName(situation.level))}</span>
                </div>
                <div class="fw-semibold text-dark small mb-1">${escapeHtml(situation.title)}</div>
                <div class="small text-muted mb-2">
                    Score: <strong>${(situation.situationScore || 0).toFixed(1)}</strong> &bull; Incidents: <strong>${situation.incidentCount}</strong>
                </div>
                
                <div class="alert alert-light border py-1 px-2 small text-muted mb-2" style="font-size: 0.72rem;">
                    <i class="bi bi-info-circle me-1"></i>Operational awareness radius: <strong>${Math.round(situation.radiusMeters)}m</strong> buffer (Advisory only)
                </div>

                <div class="mb-2">
                    <div class="small fw-semibold text-muted mb-1">Member Incidents (${situation.incidentCount}):</div>
                    <ul class="list-unstyled mb-0" style="max-height: 120px; overflow-y: auto;">
                        ${incidentsListHtml}
                    </ul>
                </div>

                <div class="d-grid mt-2">
                    <a href="/Situations/Details/${situation.id}" class="btn btn-sm btn-primary py-1">
                        <i class="bi bi-box-arrow-up-right me-1"></i>Open Situation Center
                    </a>
                </div>
            </div>
        `;

        marker.bindPopup(popupContent, { maxWidth: 350 });

        layerGroup.addLayer(circle);
        layerGroup.addLayer(marker);

        if (situationsVisible) {
            layerGroup.addTo(map);
        }

        situationLayers.set(situation.id, layerGroup);
    }

    function clearSituationLayers(map) {
        for (const layer of situationLayers.values()) {
            if (map && map.hasLayer(layer)) {
                map.removeLayer(layer);
            }
        }
        situationLayers.clear();
    }

    function toggleSituations(visible, map) {
        situationsVisible = visible;
        for (const layer of situationLayers.values()) {
            if (visible) {
                if (map && !map.hasLayer(layer)) {
                    layer.addTo(map);
                }
            } else {
                if (map && map.hasLayer(layer)) {
                    map.removeLayer(layer);
                }
            }
        }
    }

    async function refreshSituationMap() {
        const map = leafletMap || window.emergencyMapState?.map;
        if (!map) return;

        try {
            const response = await fetch('/Situations/MapSnapshot', { credentials: 'same-origin' });
            if (!response.ok) {
                console.error('Failed to load situations map snapshot:', response.status);
                return;
            }

            const situations = await response.json();
            clearSituationLayers(map);

            for (const situation of situations) {
                renderSituation(situation, map);
            }
        } catch (err) {
            console.error('Error refreshing situation map:', err);
        }
    }

    window.refreshSituationMap = refreshSituationMap;

    document.addEventListener('DOMContentLoaded', function () {
        const toggleCheckbox = document.getElementById('mapSituationsToggle');
        if (toggleCheckbox) {
            toggleCheckbox.addEventListener('change', function () {
                const map = leafletMap || window.emergencyMapState?.map;
                toggleSituations(toggleCheckbox.checked, map);
            });
        }

        // Wait for Leaflet map initialization from emergency-map.js
        let checkAttempts = 0;
        const interval = setInterval(function () {
            checkAttempts++;
            if (window.emergencyMapState && window.emergencyMapState.map) {
                clearInterval(interval);
                leafletMap = window.emergencyMapState.map;
                refreshSituationMap();
            } else if (checkAttempts > 30) {
                clearInterval(interval);
            }
        }, 200);
    });
})();
