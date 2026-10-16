/**
 * Emergency Response - Live Emergency Map (Leaflet 1.9.4 + Leaflet.markercluster 1.5.3)
 * Phase 10 Part 3: Operational Map Layer (Resources, Alert Radii, Drawer, Dashboard Focus)
 */

(function () {
    'use strict';

    var mapState = {
        allIncidents: [],
        filteredIncidents: [],
        resources: [],
        operationalZones: [],
        filters: {
            severity: '',
            priority: '',
            status: '',
            incidentType: '',
            department: '',
            aiReview: false,
            duplicateReview: false,
            visualReview: false
        }
    };

    var emergencyMap = null;
    var markersLayer = null;
    var resourceLayer = null;
    var operationalZoneLayer = null;
    var isRefreshing = false;

    function setMapRefreshStatus(state, message) {
        var status = document.getElementById('mapRefreshStatus');
        if (!status) return;
        status.dataset.state = state;
        status.textContent = message;
        status.className = 'small ' + (state === 'failed' || state === 'unavailable' ? 'text-danger' : state === 'current' ? 'text-success' : 'text-muted');
    }

    function updateMapEmptyState(message) {
        var state = document.getElementById('mapEmptyState');
        if (!state) return;
        state.textContent = message || '';
        state.hidden = !message;
    }

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

    function normalizeSeverity(val) {
        if (val === undefined || val === null) return '';
        var s = val.toString().trim().toLowerCase();
        if (s === '4' || s === 'critical') return 'Critical';
        if (s === '3' || s === 'high') return 'High';
        if (s === '2' || s === 'moderate' || s === 'medium') return 'Moderate';
        if (s === '1' || s === 'low') return 'Low';
        return val.toString().trim();
    }

    function normalizePriority(val) {
        if (val === undefined || val === null) return '';
        var s = val.toString().trim().toLowerCase();
        if (s === '4' || s === 'critical' || s === 'p1') return 'P1';
        if (s === '3' || s === 'high' || s === 'p2') return 'P2';
        if (s === '2' || s === 'normal' || s === 'medium' || s === 'p3') return 'P3';
        if (s === '1' || s === 'low' || s === 'p4') return 'P4';
        return val.toString().trim();
    }

    function normalizeStatus(val) {
        if (val === undefined || val === null) return '';
        var s = val.toString().trim();
        var statusMap = {
            '1': 'Submitted',
            '2': 'UnderReview',
            '3': 'Assigned',
            '4': 'InProgress',
            '5': 'Escalated',
            '6': 'Resolved',
            '7': 'Closed',
            '8': 'Rejected'
        };
        return statusMap[s] || s;
    }

    function getSeverityClass(severity) {
        var norm = normalizeSeverity(severity).toLowerCase();
        if (norm === 'critical') return 'critical';
        if (norm === 'high') return 'high';
        if (norm === 'low') return 'low';
        return 'medium';
    }

    function createClusterIcon(cluster) {
        var count = cluster.getChildCount();
        var size = 'small';

        if (count >= 100) {
            size = 'large';
        } else if (count >= 25) {
            size = 'medium';
        }

        return L.divIcon({
            html: '<div class="emergency-cluster emergency-cluster-' + size + '"><span>' + count + '</span></div>',
            className: '',
            iconSize: null
        });
    }

    function createMarkerIcon(severity, incident) {
        var sevClass = getSeverityClass(severity);
        var pulseHtml = (sevClass === 'critical' || sevClass === 'high')
            ? '<div class="marker-pulse"></div>'
            : '';

        var iconSymbol = sevClass === 'critical' ? '!' :
                         sevClass === 'high' ? '▲' :
                         sevClass === 'medium' ? '●' : '✓';

        var reviewRequired = incident && (
            Boolean(incident.requiresAIReview) ||
            Boolean(incident.hasDuplicateCandidates) ||
            Boolean(incident.hasVisualReview)
        );

        var alertClass = reviewRequired ? ' emergency-marker-alert' : '';

        var html = '<div class="emergency-marker marker-' + sevClass + alertClass + '">' +
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

    function getResourceIcon(resourceType, status) {
        var label = String(resourceType || 'Resource');
        var s = String(status || '').toLowerCase().replace(/\s+/g, '');
        var statusClass = 'available';
        if (s === 'assigned' || s === '1') statusClass = 'assigned';
        else if (s === 'dispatched' || s === '2') statusClass = 'dispatched';
        else if (s === 'enroute' || s === '3') statusClass = 'enroute';
        else if (s === 'onscene' || s === '4') statusClass = 'onscene';
        else if (s === 'returning' || s === '5') statusClass = 'returning';
        else if (s === 'unavailable' || s === '6') statusClass = 'unavailable';
        else if (s === 'maintenance' || s === '7') statusClass = 'maintenance';

        return L.divIcon({
            className: 'resource-marker-wrapper',
            html: '<div class="resource-marker resource-marker-' + statusClass + '" title="' + escapeHtml(label) + ' (' + escapeHtml(formatResourceStatus(status)) + ')"><span>R</span></div>',
            iconSize: [26, 26],
            iconAnchor: [13, 13],
            popupAnchor: [0, -13]
        });
    }

    function getResourceStatusBadgeClass(status) {
        var s = String(status || '').toLowerCase();
        if (s === 'available' || s === '1') return 'bg-success';
        if (s === 'assigned' || s === '2') return 'bg-warning text-dark';
        if (s === 'dispatched' || s === '3') return 'bg-warning text-dark';
        if (s === 'enroute' || s === '4') return 'bg-info text-dark';
        if (s === 'onscene' || s === '5') return 'bg-primary';
        if (s === 'returning' || s === '6') return 'bg-info text-dark';
        if (s === 'unavailable' || s === '7') return 'bg-danger';
        if (s === 'maintenance' || s === '8') return 'bg-secondary';
        return 'bg-secondary';
    }

    function formatResourceStatus(status) {
        var s = String(status || '');
        var map = {
            '1': 'Available',
            '2': 'Assigned',
            '3': 'Dispatched',
            '4': 'En Route',
            '5': 'On Scene',
            '6': 'Returning',
            '7': 'Unavailable',
            '8': 'Maintenance'
        };
        if (map[s]) return map[s];
        if (s.toLowerCase() === 'enroute') return 'En Route';
        if (s.toLowerCase() === 'onscene') return 'On Scene';
        return s;
    }

    function createPopupContent(incident) {
        var title = escapeHtml(incident.title || 'Emergency Incident');
        var incNumber = escapeHtml(incident.incidentNumber || ('INC-' + incident.id));
        var incType = escapeHtml(incident.incidentType || 'Emergency');
        var dept = incident.departmentName ? escapeHtml(incident.departmentName) : 'General Dispatch';
        var status = escapeHtml(normalizeStatus(incident.status));
        var severity = escapeHtml(normalizeSeverity(incident.severity));
        var address = escapeHtml(incident.address || '');
        var detailsUrl = '/Incidents/Details/' + incident.id;

        var sevBadgeClass = 'bg-secondary';
        var sevClass = getSeverityClass(incident.severity);
        if (sevClass === 'critical') sevBadgeClass = 'bg-danger';
        else if (sevClass === 'high') sevBadgeClass = 'bg-warning text-dark';
        else if (sevClass === 'medium') sevBadgeClass = 'bg-info text-dark';
        else if (sevClass === 'low') sevBadgeClass = 'bg-success';

        var reviewBadges = '';
        if (incident.hasDuplicateCandidates) {
            reviewBadges += '<span class="badge bg-warning text-dark me-1" title="Possible duplicate incident detected">Duplicate review</span>';
        }
        if (incident.hasVisualReview) {
            reviewBadges += '<span class="badge bg-info text-dark me-1" title="Visual review required or completed">Visual review</span>';
        }
        if (incident.requiresAIReview) {
            reviewBadges += '<span class="badge bg-secondary me-1" title="Requires AI review">AI review</span>';
        }

        var locationHtml = address ? '<div class="text-muted small mb-1"><i class="bi bi-geo-alt me-1"></i>' + address + '</div>' : '';

        var deptHtml = incident.departmentName
            ? '<div class="small mb-1"><strong>Department:</strong> ' + escapeHtml(incident.departmentName) + '</div>'
            : '';

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
                '<div class="small mb-1"><strong>Type:</strong> ' + incType + '</div>' +
                deptHtml +
                '<div class="small mb-2"><strong>Status:</strong> <span class="badge bg-light text-dark border">' + status + '</span></div>' +
                (reviewBadges ? '<div class="map-popup-badges mb-2">' + reviewBadges + '</div>' : '') +
            '</div>' +
            '<div class="map-popup-footer">' +
                '<span class="text-muted small">' + (incident.createdAtUtc ? new Date(incident.createdAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '') + '</span>' +
                '<div class="btn-group btn-group-sm">' +
                    '<button type="button" class="btn btn-sm btn-outline-secondary py-0 px-2 btn-open-drawer" data-incident-id="' + incident.id + '">Drawer</button>' +
                    '<a href="' + detailsUrl + '" class="btn btn-sm btn-primary py-0 px-2" target="_blank">Details</a>' +
                '</div>' +
            '</div>' +
        '</div>';

        return html;
    }

    function matchesMapFilters(incident) {
        var filters = mapState.filters;

        if (filters.severity && normalizeSeverity(incident.severity) !== filters.severity) {
            return false;
        }

        if (filters.priority && normalizePriority(incident.priority) !== filters.priority) {
            return false;
        }

        if (filters.status && normalizeStatus(incident.status) !== filters.status) {
            return false;
        }

        if (filters.incidentType && (incident.incidentType || '') !== filters.incidentType) {
            return false;
        }

        if (filters.department && (incident.departmentName || '') !== filters.department) {
            return false;
        }

        if (filters.aiReview && !incident.requiresAIReview) {
            return false;
        }

        if (filters.duplicateReview && !incident.hasDuplicateCandidates) {
            return false;
        }

        if (filters.visualReview && !incident.hasVisualReview) {
            return false;
        }

        return true;
    }

    function readMapFilters() {
        mapState.filters.severity = document.getElementById('mapSeverityFilter')?.value || '';
        mapState.filters.priority = document.getElementById('mapPriorityFilter')?.value || '';
        mapState.filters.status = document.getElementById('mapStatusFilter')?.value || '';
        mapState.filters.incidentType = document.getElementById('mapIncidentTypeFilter')?.value || '';
        mapState.filters.department = document.getElementById('mapDepartmentFilter')?.value || '';
        mapState.filters.aiReview = Boolean(document.getElementById('mapAIReviewFilter')?.checked);
        mapState.filters.duplicateReview = Boolean(document.getElementById('mapDuplicateFilter')?.checked);
        mapState.filters.visualReview = Boolean(document.getElementById('mapVisualFilter')?.checked);
    }

    function updateIncidentCount(filteredCount) {
        var total = mapState.allIncidents.length;
        var countEl = document.getElementById('mapIncidentCount');
        if (countEl) {
            countEl.textContent = filteredCount + ' / ' + total + ' incidents';
        }
    }

    function updateFilterSummary() {
        var element = document.getElementById('activeMapFilterSummary');
        if (!element) return;

        var parts = [];
        var f = mapState.filters;

        if (f.severity) parts.push('Severity: ' + f.severity);
        if (f.priority) parts.push('Priority: ' + f.priority);
        if (f.status) parts.push('Status: ' + f.status);
        if (f.incidentType) parts.push('Type: ' + f.incidentType);
        if (f.department) parts.push('Dept: ' + f.department);
        if (f.aiReview) parts.push('AI Review');
        if (f.duplicateReview) parts.push('Duplicate Review');
        if (f.visualReview) parts.push('Visual Review');

        element.textContent = parts.length === 0
            ? 'Showing all active incidents.'
            : 'Filters: ' + parts.join(' • ');
    }

    function applyMapFilters(fitToBounds) {
        readMapFilters();
        mapState.filteredIncidents = mapState.allIncidents.filter(matchesMapFilters);

        renderIncidents(mapState.filteredIncidents, Boolean(fitToBounds));
        updateIncidentCount(mapState.filteredIncidents.length);
        updateFilterSummary();
    }

    function clearMapFilters() {
        var sev = document.getElementById('mapSeverityFilter');
        var prio = document.getElementById('mapPriorityFilter');
        var stat = document.getElementById('mapStatusFilter');
        var type = document.getElementById('mapIncidentTypeFilter');
        var dept = document.getElementById('mapDepartmentFilter');
        var ai = document.getElementById('mapAIReviewFilter');
        var dup = document.getElementById('mapDuplicateFilter');
        var vis = document.getElementById('mapVisualFilter');

        if (sev) sev.value = '';
        if (prio) prio.value = '';
        if (stat) stat.value = '';
        if (type) type.value = '';
        if (dept) dept.value = '';
        if (ai) ai.checked = false;
        if (dup) dup.checked = false;
        if (vis) vis.checked = false;

        mapState.filters = {
            severity: '',
            priority: '',
            status: '',
            incidentType: '',
            department: '',
            aiReview: false,
            duplicateReview: false,
            visualReview: false
        };

        mapState.filteredIncidents = [].concat(mapState.allIncidents);
        renderIncidents(mapState.filteredIncidents, false);
        updateIncidentCount(mapState.filteredIncidents.length);
        updateFilterSummary();
    }

    function fitFilteredIncidents() {
        if (!emergencyMap) return;

        var bounds = L.latLngBounds();
        var count = 0;

        (mapState.filteredIncidents || []).forEach(function (inc) {
            if (isValidCoordinate(inc.latitude, inc.longitude)) {
                bounds.extend([inc.latitude, inc.longitude]);
                count++;
            }
        });

        if (count > 0 && bounds.isValid()) {
            emergencyMap.fitBounds(bounds, { padding: [30, 30], maxZoom: 14 });
        }
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
            var icon = createMarkerIcon(inc.severity, inc);
            var marker = L.marker(latLng, { icon: icon });

            var popupContent = createPopupContent(inc);
            marker.bindPopup(popupContent, { minWidth: 260 });

            // Marker click opens the incident drawer
            marker.on('click', function () {
                openIncidentDrawer(inc.id);
            });

            markersLayer.addLayer(marker);
            bounds.extend(latLng);

            total++;
            var s = getSeverityClass(inc.severity);
            if (s === 'critical') critical++;
            else if (s === 'high') high++;
            else if (s === 'low') low++;
            else medium++;
        });

        updateMapEmptyState(total > 0 ? '' : (incidents && incidents.length > 0
            ? 'No incident coordinates are available for the current results.'
            : 'No incidents match the current filters.'));

        // Update stats on DOM
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

    function renderResources(resources) {
        if (!emergencyMap || !resourceLayer) return;

        resourceLayer.clearLayers();
        mapState.resources = resources || [];

        var count = 0;
        (resources || []).forEach(function (res) {
            if (!isValidCoordinate(res.latitude, res.longitude)) {
                return;
            }

            var latLng = [res.latitude, res.longitude];
            var icon = getResourceIcon(res.resourceType, res.status);
            var marker = L.marker(latLng, { icon: icon });

            var deptHtml = res.departmentName
                ? '<div><strong>Department:</strong> ' + escapeHtml(res.departmentName) + '</div>'
                : '';

            var updatedHtml = res.locationUpdatedAtUtc
                ? '<div class="text-muted small mt-1">Updated: ' + new Date(res.locationUpdatedAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + '</div>'
                : '';

            var statusClass = getResourceStatusBadgeClass(res.status);
            var statusText = formatResourceStatus(res.status);

            var milestoneHtml = '';
            if (res.onSceneAtUtc) {
                milestoneHtml = '<div class="small text-muted mb-1">On Scene: ' + new Date(res.onSceneAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + '</div>';
            } else if (res.enRouteAtUtc) {
                milestoneHtml = '<div class="small text-muted mb-1">En Route: ' + new Date(res.enRouteAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + '</div>';
            } else if (res.dispatchedAtUtc) {
                milestoneHtml = '<div class="small text-muted mb-1">Dispatched: ' + new Date(res.dispatchedAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + '</div>';
            } else if (res.assignedAtUtc) {
                milestoneHtml = '<div class="small text-muted mb-1">Assigned: ' + new Date(res.assignedAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) + '</div>';
            }

            var incidentHtml = res.activeIncidentNumber
                ? '<div class="mb-1"><strong>Incident:</strong> <a href="/Incidents/Details/' + encodeURIComponent(res.activeIncidentId || '') + '" class="badge bg-primary text-decoration-none" title="View incident details">' + escapeHtml(res.activeIncidentNumber) + '</a></div>'
                : '';

            marker.bindPopup(
                '<div class="resource-popup">' +
                    '<div class="fw-bold fs-6">' + escapeHtml(res.name) + '</div>' +
                    '<div class="small text-muted mb-1">' + escapeHtml(res.resourceType) + '</div>' +
                    '<div class="mb-1"><strong>Status:</strong> <span class="badge ' + statusClass + '">' + escapeHtml(statusText) + '</span></div>' +
                    milestoneHtml +
                    incidentHtml +
                    deptHtml +
                    updatedHtml +
                    '<div class="mt-2 pt-1 border-top"><a href="/Resources/Details/' + encodeURIComponent(res.id) + '" class="btn btn-sm btn-outline-primary w-100 py-0" style="font-size: 0.75rem;">View Command Details</a></div>' +
                '</div>'
            );

            marker._resourceId = res.id;
            resourceLayer.addLayer(marker);
            count++;
        });

        updateBadge('statResources', count);
    }

    function renderOperationalZones(zones) {
        if (!emergencyMap || !operationalZoneLayer) return;

        operationalZoneLayer.clearLayers();
        mapState.operationalZones = zones || [];

        (zones || []).forEach(function (zone) {
            if (!isValidCoordinate(zone.latitude, zone.longitude)) {
                return;
            }

            if (!Number.isFinite(zone.radiusMeters) || zone.radiusMeters <= 0) {
                return;
            }

            var sevClass = getSeverityClass(zone.severity);
            var color = sevClass === 'critical' ? '#dc3545' : (sevClass === 'high' ? '#fd7e14' : '#ffc107');

            var circle = L.circle([zone.latitude, zone.longitude], {
                radius: zone.radiusMeters,
                color: color,
                weight: 2,
                fillColor: color,
                fillOpacity: 0.08
            });

            circle.bindTooltip(
                escapeHtml(zone.severity) + ' — operational alert radius (' + Math.round(zone.radiusMeters) + 'm)'
            );

            operationalZoneLayer.addLayer(circle);
        });
    }

    async function openIncidentDrawer(incidentId) {
        var content = document.getElementById('incidentDetailsContent');
        var drawerElement = document.getElementById('incidentDetailsDrawer');
        if (!content || !drawerElement) return;

        content.innerHTML =
            '<div class="text-center py-5">' +
                '<div class="spinner-border text-primary" role="status"></div>' +
                '<div class="mt-2 text-muted">Loading incident details...</div>' +
            '</div>';

        if (window.bootstrap && window.bootstrap.Offcanvas) {
            var drawer = bootstrap.Offcanvas.getOrCreateInstance(drawerElement);
            drawer.show();
        }

        try {
            var response = await fetch('/Map/Incident/' + incidentId, {
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) {
                throw new Error('Incident fetch failed with status: ' + response.status);
            }

            var incident = await response.json();
            renderIncidentDrawer(incident);
        } catch (err) {
            console.error('[EmergencyMap] Incident drawer failed:', err);
            content.innerHTML =
                '<div class="alert alert-danger my-4">' +
                    'Incident details could not be loaded.' +
                '</div>';
        }
    }

    function renderIncidentDrawer(incident) {
        var content = document.getElementById('incidentDetailsContent');
        if (!content || !incident) return;

        var incNumber = escapeHtml(incident.incidentNumber || ('INC-' + incident.id));
        var title = escapeHtml(incident.title || 'Emergency Incident');
        var severity = escapeHtml(normalizeSeverity(incident.severity));
        var priority = escapeHtml(normalizePriority(incident.priority));
        var status = escapeHtml(normalizeStatus(incident.status));
        var type = escapeHtml(incident.incidentTypeName || incident.incidentType || 'Emergency');
        var description = escapeHtml(incident.description || 'No description provided.');
        var address = escapeHtml(incident.location?.address || incident.address || 'Location coordinates on map');
        var dept = incident.departmentName ? escapeHtml(incident.departmentName) : 'General Dispatch';

        var sevBadgeClass = 'bg-secondary';
        var sClass = getSeverityClass(incident.severity);
        if (sClass === 'critical') sevBadgeClass = 'bg-danger';
        else if (sClass === 'high') sevBadgeClass = 'bg-warning text-dark';
        else if (sClass === 'medium') sevBadgeClass = 'bg-info text-dark';
        else if (sClass === 'low') sevBadgeClass = 'bg-success';

        content.innerHTML =
            '<div>' +
                '<div class="mb-3">' +
                    '<div class="small text-muted">Incident Number</div>' +
                    '<div class="fs-5 fw-bold">' + incNumber + '</div>' +
                '</div>' +

                '<div class="mb-3">' +
                    '<div class="small text-muted">Title</div>' +
                    '<div class="fw-semibold">' + title + '</div>' +
                '</div>' +

                '<div class="row g-2 mb-3">' +
                    '<div class="col-6">' +
                        '<div class="small text-muted">Severity</div>' +
                        '<div><span class="badge ' + sevBadgeClass + '">' + severity + '</span></div>' +
                    '</div>' +
                    '<div class="col-6">' +
                        '<div class="small text-muted">Priority</div>' +
                        '<div class="fw-semibold">' + priority + '</div>' +
                    '</div>' +
                    '<div class="col-6">' +
                        '<div class="small text-muted">Status</div>' +
                        '<div><span class="badge bg-light text-dark border">' + status + '</span></div>' +
                    '</div>' +
                    '<div class="col-6">' +
                        '<div class="small text-muted">Incident Type</div>' +
                        '<div>' + type + '</div>' +
                    '</div>' +
                '</div>' +

                '<div class="mb-3">' +
                    '<div class="small text-muted">Department</div>' +
                    '<div>' + dept + '</div>' +
                '</div>' +

                '<div class="mb-3">' +
                    '<div class="small text-muted">Location</div>' +
                    '<div><i class="bi bi-geo-alt me-1 text-danger"></i>' + address + '</div>' +
                '</div>' +

                '<div class="mb-3">' +
                    '<div class="small text-muted">Description</div>' +
                    '<div class="p-2 bg-light rounded small">' + description + '</div>' +
                '</div>' +

                '<div class="d-grid gap-2 mt-4">' +
                    '<a href="/Incidents/Details/' + incident.id + '" class="btn btn-primary btn-sm" target="_blank">' +
                        'Open Full Incident' +
                    '</a>' +
                    '<a href="/Dashboard?incidentId=' + encodeURIComponent(incident.id) + '" class="btn btn-outline-primary btn-sm">' +
                        'Open in Dashboard' +
                    '</a>' +
                    '<button type="button" class="btn btn-outline-secondary btn-sm btn-center-on-map" data-incident-id="' + incident.id + '">' +
                        'Center on Map' +
                    '</button>' +
                '</div>' +
            '</div>';

        var centerBtn = content.querySelector('.btn-center-on-map');
        if (centerBtn) {
            centerBtn.addEventListener('click', function () {
                focusSelectedMapIncident(incident.id);
            });
        }
    }

    function focusSelectedMapIncident(incidentId) {
        if (!emergencyMap || !incidentId) return;

        var inc = mapState.allIncidents.find(function (x) {
            return String(x.id).toLowerCase() === String(incidentId).toLowerCase();
        });

        if (!inc || !isValidCoordinate(inc.latitude, inc.longitude)) {
            return;
        }

        emergencyMap.setView([inc.latitude, inc.longitude], 15);
    }

    function focusSelectedMapResource(resourceId) {
        if (!emergencyMap || !resourceId) return;

        var res = mapState.resources.find(function (x) {
            return String(x.id).toLowerCase() === String(resourceId).toLowerCase();
        });

        if (!res || !isValidCoordinate(res.latitude, res.longitude)) {
            return;
        }

        emergencyMap.setView([res.latitude, res.longitude], 16);

        if (resourceLayer) {
            resourceLayer.eachLayer(function (layer) {
                if (layer._resourceId && String(layer._resourceId).toLowerCase() === String(resourceId).toLowerCase()) {
                    layer.openPopup();
                }
            });
        }
    }

    function updateBadge(id, count) {
        var el = document.getElementById(id);
        if (el) {
            el.textContent = count;
        }
    }

    function initMap(config) {
        var mapElement = document.getElementById('emergencyMap');
        if (!mapElement) return;

        var defaultLat = config.defaultLatitude || 20.5937;
        var defaultLng = config.defaultLongitude || 78.9629;
        var defaultZoom = config.defaultZoom || 5;
        var tileUrl = config.tileUrl || 'https://tile.openstreetmap.org/{z}/{x}/{y}.png';
        var attribution = config.attribution || '&copy; <a href="https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors';

        emergencyMap = L.map('emergencyMap', {
            center: [defaultLat, defaultLng],
            zoom: defaultZoom,
            zoomControl: true
        });

        L.tileLayer(tileUrl, {
            attribution: attribution,
            maxZoom: 19
        }).addTo(emergencyMap);
        emergencyMap.on('tileerror', function () {
            setMapRefreshStatus('unavailable', 'Basemap tiles are unavailable; incident data may still be shown.');
        });

        // Incident MarkerClusterGroup
        markersLayer = L.markerClusterGroup({
            maxClusterRadius: 60,
            disableClusteringAtZoom: 15,
            showCoverageOnHover: false,
            zoomToBoundsOnClick: true,
            spiderfyOnMaxZoom: true,
            removeOutsideVisibleBounds: true,
            chunkedLoading: true,
            iconCreateFunction: createClusterIcon
        }).addTo(emergencyMap);

        // Resource and Operational Radii layers
        resourceLayer = L.layerGroup().addTo(emergencyMap);
        operationalZoneLayer = L.layerGroup().addTo(emergencyMap);

        // Layer Control
        L.control.layers(
            null,
            {
                'Incidents': markersLayer,
                'Resources': resourceLayer,
                'Operational alert radii': operationalZoneLayer
            },
            { collapsed: false }
        ).addTo(emergencyMap);

        // Map Legend
        var legendControl = L.control({ position: 'bottomright' });
        legendControl.onAdd = function () {
            var div = L.DomUtil.create('div', 'map-legend');
            div.innerHTML =
                '<strong>Incident Severity</strong><br/>' +
                '<div class="map-legend-item"><span class="map-legend-dot dot-critical"></span> Critical (<span id="legendCountCritical">0</span>)</div>' +
                '<div class="map-legend-item"><span class="map-legend-dot dot-high"></span> High (<span id="legendCountHigh">0</span>)</div>' +
                '<div class="map-legend-item"><span class="map-legend-dot dot-medium"></span> Medium (<span id="legendCountMedium">0</span>)</div>' +
                '<div class="map-legend-item"><span class="map-legend-dot dot-low"></span> Low (<span id="legendCountLow">0</span>)</div>' +
                '<div class="map-legend-item pt-1 border-top mt-1"><span class="resource-marker-mini"></span> Resource</div>' +
                '<div class="map-legend-item"><span class="zone-marker-mini"></span> Operational radius</div>' +
                '<div class="map-legend-item"><span class="situation-marker-mini">!</span> Situation marker</div>';
            return div;
        };
        legendControl.addTo(emergencyMap);

        // Initial Data
        mapState.allIncidents = Array.isArray(config.initialIncidents) ? config.initialIncidents : [];
        mapState.filteredIncidents = [].concat(mapState.allIncidents);
        mapState.resources = Array.isArray(config.initialResources) ? config.initialResources : [];
        mapState.operationalZones = Array.isArray(config.initialOperationalZones) ? config.initialOperationalZones : [];

        renderIncidents(mapState.filteredIncidents, true);
        renderResources(mapState.resources);
        renderOperationalZones(mapState.operationalZones);

        updateIncidentCount(mapState.filteredIncidents.length);
        updateFilterSummary();

        // Focus selected incident or resource if requested via query parameter
        var pageElement = document.getElementById('emergencyMapPage');
        var selectedId = config.selectedIncidentId || pageElement?.dataset?.selectedIncidentId;
        if (selectedId) {
            setTimeout(function () {
                focusSelectedMapIncident(selectedId);
            }, 300);
        }

        var selectedResId = config.selectedResourceId || pageElement?.dataset?.selectedResourceId;
        if (selectedResId) {
            setTimeout(function () {
                focusSelectedMapResource(selectedResId);
            }, 300);
        }

        // Export Global State
        window.emergencyMapState = {
            map: emergencyMap,
            markersLayer: markersLayer,
            resourceLayer: resourceLayer,
            operationalZoneLayer: operationalZoneLayer,
            mapState: mapState,
            renderIncidents: renderIncidents,
            renderResources: renderResources,
            renderOperationalZones: renderOperationalZones,
            refreshMap: refreshMap,
            applyMapFilters: applyMapFilters,
            clearMapFilters: clearMapFilters,
            fitFilteredIncidents: fitFilteredIncidents,
            openIncidentDrawer: openIncidentDrawer,
            focusSelectedMapIncident: focusSelectedMapIncident,
            focusSelectedMapResource: focusSelectedMapResource
        };
    }

    async function refreshMap() {
        if (isRefreshing) return;
        isRefreshing = true;

        var refreshSpinner = document.getElementById('mapRefreshSpinner');
        var refreshButton = document.getElementById('btnRefreshMap');
        if (refreshSpinner) refreshSpinner.classList.remove('d-none');
        if (refreshButton) refreshButton.disabled = true;
        setMapRefreshStatus('loading', 'Refreshing snapshot…');

        try {
            var response = await fetch('/Map/Snapshot', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json'
                }
            });

            if (!response.ok) throw new Error('Snapshot request failed (' + response.status + ').');
            var data = await response.json();
            if (!data || !Array.isArray(data.incidents) || !Array.isArray(data.resources) ||
                !Array.isArray(data.operationalZones) || !data.updatedAtUtc || !Number.isFinite(Date.parse(data.updatedAtUtc))) {
                throw new Error('Snapshot response was incomplete.');
            }
            mapState.allIncidents = data.incidents;
            mapState.resources = data.resources;
            mapState.operationalZones = data.operationalZones;

            // Re-apply active filters on the updated authoritative snapshot.
            mapState.filteredIncidents = mapState.allIncidents.filter(matchesMapFilters);

            renderIncidents(mapState.filteredIncidents, false);
            renderResources(mapState.resources);
            renderOperationalZones(mapState.operationalZones);

            updateIncidentCount(mapState.filteredIncidents.length);
            updateFilterSummary();

            var lastUpdatedEl = document.getElementById('mapLastUpdated');
            if (lastUpdatedEl) {
                lastUpdatedEl.textContent = 'Snapshot retrieved ' + new Date(data.updatedAtUtc).toLocaleString('en-GB', { timeZone: 'UTC' }) + ' UTC';
            }
            setMapRefreshStatus('current', 'Snapshot current');
        } catch (err) {
            setMapRefreshStatus('failed', 'Refresh failed. Showing the last retrieved snapshot.');
            console.error('[EmergencyMap] Error refreshing map snapshot:', err);
        } finally {
            isRefreshing = false;
            if (refreshSpinner) refreshSpinner.classList.add('d-none');
            if (refreshButton) refreshButton.disabled = false;
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        var configEl = document.getElementById('emergencyMapConfig');
        if (configEl) {
            try {
                var config = JSON.parse(configEl.textContent || '{}');
                initMap(config);
            } catch (e) {
                updateMapEmptyState('Map unavailable. Snapshot data could not be loaded.');
                setMapRefreshStatus('unavailable', 'Map unavailable');
                console.error('[EmergencyMap] Failed to parse map config JSON:', e);
            }
        }

        var refreshBtn = document.getElementById('btnRefreshMap');
        if (refreshBtn) {
            refreshBtn.addEventListener('click', function () {
                refreshMap();
            });
        }

        // Action Buttons
        var applyBtn = document.getElementById('applyMapFilters');
        if (applyBtn) {
            applyBtn.addEventListener('click', function () {
                applyMapFilters(false);
            });
        }

        var clearBtn = document.getElementById('clearMapFilters');
        if (clearBtn) {
            clearBtn.addEventListener('click', function () {
                clearMapFilters();
            });
        }

        var fitBtn = document.getElementById('fitFilteredIncidents');
        if (fitBtn) {
            fitBtn.addEventListener('click', function () {
                fitFilteredIncidents();
            });
        }

        // Layer Toggles
        var resToggle = document.getElementById('mapResourcesToggle');
        if (resToggle) {
            resToggle.addEventListener('change', function (e) {
                if (emergencyMap && resourceLayer) {
                    if (e.target.checked) emergencyMap.addLayer(resourceLayer);
                    else emergencyMap.removeLayer(resourceLayer);
                }
            });
        }

        var zoneToggle = document.getElementById('mapZonesToggle');
        if (zoneToggle) {
            zoneToggle.addEventListener('change', function (e) {
                if (emergencyMap && operationalZoneLayer) {
                    if (e.target.checked) emergencyMap.addLayer(operationalZoneLayer);
                    else emergencyMap.removeLayer(operationalZoneLayer);
                }
            });
        }

        // Popup delegated clicks to open drawer
        document.addEventListener('click', function (e) {
            var drawerBtn = e.target.closest('.btn-open-drawer');
            if (drawerBtn) {
                var incId = drawerBtn.getAttribute('data-incident-id');
                if (incId) {
                    openIncidentDrawer(incId);
                }
            }
        });

        // Instant reactivity on select dropdown change
        [
            'mapSeverityFilter',
            'mapPriorityFilter',
            'mapStatusFilter',
            'mapIncidentTypeFilter',
            'mapDepartmentFilter'
        ].forEach(function (id) {
            var el = document.getElementById(id);
            if (el) {
                el.addEventListener('change', function () {
                    applyMapFilters(false);
                });
            }
        });

        // Instant reactivity on checkbox toggle
        [
            'mapAIReviewFilter',
            'mapDuplicateFilter',
            'mapVisualFilter'
        ].forEach(function (id) {
            var el = document.getElementById(id);
            if (el) {
                el.addEventListener('change', function () {
                    applyMapFilters(false);
                });
            }
        });
    });
})();
