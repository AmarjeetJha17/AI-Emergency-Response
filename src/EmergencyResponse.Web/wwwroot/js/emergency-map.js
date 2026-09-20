/**
 * Emergency Response - Live Emergency Map (Leaflet 1.9.4)
 */

(function () {
    'use strict';

    function escapeHtml(str) {
        if (!str) return '';
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function isValidCoordinate(lat, lng) {
        return typeof lat === 'number' && typeof lng === 'number' &&
            !isNaN(lat) && !isNaN(lng) &&
            lat >= -90 && lat <= 90 &&
            lng >= -180 && lng <= 180 &&
            !(lat === 0 && lng === 0);
    }

    function getSeverityClass(severity) {
        if (!severity) return 'medium';
        var s = severity.toString().toLowerCase();
        if (s.indexOf('crit') !== -1) return 'critical';
        if (s.indexOf('high') !== -1) return 'high';
        if (s.indexOf('low') !== -1) return 'low';
        return 'medium';
    }

    function createMarkerIcon(severity) {
        var sevClass = getSeverityClass(severity);
        var pulseHtml = (sevClass === 'critical' || sevClass === 'high')
            ? '<div class="marker-pulse"></div>'
            : '';

        var iconSymbol = sevClass === 'critical' ? '!' :
                         sevClass === 'high' ? '▲' :
                         sevClass === 'medium' ? '●' : '✓';

        var html = '<div class="emergency-marker marker-' + sevClass + '">' +
                   pulseHtml +
                   '<div class="marker-pin"><span class="marker-icon-content">' + iconSymbol + '</span></div>' +
                   '</div>';

        return L.divIcon({
            className: 'emergency-marker-wrapper',
            html: html,
            iconSize: [32, 32],
            iconAnchor: [16, 32],
            popupAnchor: [0, -32]
        });
    }

    function createPopupContent(incident) {
        var title = escapeHtml(incident.title || 'Emergency Incident');
        var incNumber = escapeHtml(incident.incidentNumber || ('INC-' + incident.id));
        var incType = escapeHtml(incident.incidentType || 'Emergency');
        var dept = escapeHtml(incident.departmentName || 'General Dispatch');
        var status = escapeHtml(incident.status || 'Active');
        var severity = escapeHtml(incident.severity || 'Medium');
        var address = escapeHtml(incident.address || '');
        var detailsUrl = '/Incidents/Details/' + incident.id;

        var sevBadgeClass = 'bg-secondary';
        var sevClass = getSeverityClass(incident.severity);
        if (sevClass === 'critical') sevBadgeClass = 'bg-danger';
        else if (sevClass === 'high') sevBadgeClass = 'bg-warning text-dark';
        else if (sevClass === 'medium') sevBadgeClass = 'bg-info text-dark';
        else if (sevClass === 'low') sevBadgeClass = 'bg-success';

        var reviewBadges = '';
        if (incident.requiresAIReview) {
            reviewBadges += '<span class="badge bg-warning text-dark me-1" title="Requires AI review">AI Review</span>';
        }
        if (incident.hasDuplicateCandidates) {
            reviewBadges += '<span class="badge bg-info text-dark me-1" title="Possible duplicate incident detected">Duplicate</span>';
        }
        if (incident.hasVisualReview) {
            reviewBadges += '<span class="badge text-white me-1" style="background-color:#6f42c1" title="Visual review completed">Visual Analysis</span>';
        }

        var locationHtml = address ? '<div class="text-muted small mb-1"><i class="bi bi-geo-alt me-1"></i>' + address + '</div>' : '';

        var html = '<div class="map-popup-card">' +
            '<div class="map-popup-header">' +
                '<div class="d-flex justify-content-between align-items-center">' +
                    '<span class="map-popup-incident-number">' + incNumber + '</span>' +
                    '<span class="badge ' + sevBadgeClass + '">' + severity + '</span>' +
                '</div>' +
                '<div class="map-popup-title">' + title + '</div>' +
            '</div>' +
            '<div class="map-popup-body">' +
                locationHtml +
                '<div class="small mb-2"><strong>Type:</strong> ' + incType + ' &bull; <strong>Dept:</strong> ' + dept + '</div>' +
                '<div class="small mb-2"><strong>Status:</strong> <span class="badge bg-light text-dark border">' + status + '</span></div>' +
                (reviewBadges ? '<div class="map-popup-badges mb-2">' + reviewBadges + '</div>' : '') +
            '</div>' +
            '<div class="map-popup-footer">' +
                '<span class="text-muted small">' + (incident.createdAtUtc ? new Date(incident.createdAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '') + '</span>' +
                '<a href="' + detailsUrl + '" class="btn btn-sm btn-primary py-0 px-2" target="_blank">View Details</a>' +
            '</div>' +
        '</div>';

        return html;
    }

    var emergencyMap = null;
    var markersLayer = null;

    function initMap(config) {
        var mapElement = document.getElementById('emergencyMap');
        if (!mapElement) return;

        var defaultLat = config.defaultLatitude || 20.5937;
        var defaultLng = config.defaultLongitude || 78.9629;
        var defaultZoom = config.defaultZoom || 5;
        var tileUrl = config.tileUrl || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
        var attribution = config.attribution || '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';

        emergencyMap = L.map('emergencyMap', {
            center: [defaultLat, defaultLng],
            zoom: defaultZoom,
            zoomControl: true
        });

        L.tileLayer(tileUrl, {
            attribution: attribution,
            maxZoom: 19
        }).addTo(emergencyMap);

        markersLayer = L.layerGroup().addTo(emergencyMap);

        // Add Map Legend
        var legendControl = L.control({ position: 'bottomright' });
        legendControl.onAdd = function () {
            var div = L.DomUtil.create('div', 'map-legend');
            div.innerHTML =
                '<strong>Incident Severity</strong><br/>' +
                '<div class="map-legend-item"><span class="map-legend-dot dot-critical"></span> Critical (<span id="legendCountCritical">0</span>)</div>' +
                '<div class="map-legend-item"><span class="map-legend-dot dot-high"></span> High (<span id="legendCountHigh">0</span>)</div>' +
                '<div class="map-legend-item"><span class="map-legend-dot dot-medium"></span> Medium (<span id="legendCountMedium">0</span>)</div>' +
                '<div class="map-legend-item"><span class="map-legend-dot dot-low"></span> Low (<span id="legendCountLow">0</span>)</div>';
            return div;
        };
        legendControl.addTo(emergencyMap);

        // Render initial incidents from server
        if (config.initialIncidents && Array.isArray(config.initialIncidents)) {
            renderIncidents(config.initialIncidents, true);
        }

        // Global State Export
        window.emergencyMapState = {
            map: emergencyMap,
            markersLayer: markersLayer,
            renderIncidents: renderIncidents,
            refreshMap: refreshMap
        };
    }

    function renderIncidents(incidents, fitToBounds) {
        if (!emergencyMap || !markersLayer) return;

        markersLayer.clearLayers();

        var total = 0;
        var critical = 0;
        var high = 0;
        var medium = 0;
        var low = 0;

        var bounds = L.latLngBounds();

        (incidents || []).forEach(function (inc) {
            if (!isValidCoordinate(inc.latitude, inc.longitude)) {
                return;
            }

            var latLng = [inc.latitude, inc.longitude];
            var icon = createMarkerIcon(inc.severity);
            var marker = L.marker(latLng, { icon: icon });

            var popupContent = createPopupContent(inc);
            marker.bindPopup(popupContent, { minWidth: 260 });

            markersLayer.addLayer(marker);
            bounds.extend(latLng);

            total++;
            var s = getSeverityClass(inc.severity);
            if (s === 'critical') critical++;
            else if (s === 'high') high++;
            else if (s === 'low') low++;
            else medium++;
        });

        // Update stats on DOM
        updateBadge('statTotal', total);
        updateBadge('statCritical', critical);
        updateBadge('statHigh', high);
        updateBadge('statMedium', medium);
        updateBadge('statLow', low);

        updateBadge('legendCountCritical', critical);
        updateBadge('legendCountHigh', high);
        updateBadge('legendCountMedium', medium);
        updateBadge('legendCountLow', low);

        if (fitToBounds && total > 0 && bounds.isValid()) {
            emergencyMap.fitBounds(bounds, { padding: [50, 50], maxZoom: 14 });
        }
    }

    function updateBadge(id, count) {
        var el = document.getElementById(id);
        if (el) {
            el.textContent = count;
        }
    }

    var isRefreshing = false;

    async function refreshMap() {
        if (isRefreshing) return;
        isRefreshing = true;

        var refreshSpinner = document.getElementById('mapRefreshSpinner');
        if (refreshSpinner) refreshSpinner.classList.remove('d-none');

        try {
            var response = await fetch('/Map/Snapshot', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (response.ok) {
                var data = await response.json();
                if (data && data.incidents) {
                    renderIncidents(data.incidents, false); // Keep pan & zoom intact
                }

                var lastUpdatedEl = document.getElementById('mapLastUpdated');
                if (lastUpdatedEl) {
                    lastUpdatedEl.textContent = 'Updated ' + new Date().toLocaleTimeString();
                }
            } else {
                console.warn('[EmergencyMap] Snapshot fetch failed with status:', response.status);
            }
        } catch (err) {
            console.error('[EmergencyMap] Error refreshing map snapshot:', err);
        } finally {
            isRefreshing = false;
            if (refreshSpinner) refreshSpinner.classList.add('d-none');
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        var configEl = document.getElementById('emergencyMapConfig');
        if (configEl) {
            try {
                var config = JSON.parse(configEl.textContent || '{}');
                initMap(config);
            } catch (e) {
                console.error('[EmergencyMap] Failed to parse map config JSON:', e);
            }
        }

        var refreshBtn = document.getElementById('btnRefreshMap');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', function () {
                refreshMap();
            });
        }
    });
})();
