(function () {
    const apiBase = document.currentScript?.dataset.apiBase ?? '';

    function getToken() {
        const meta = document.querySelector('meta[name="signalr-token"]');
        return meta ? meta.content : '';
    }

    const token = getToken();
    if (!token) {
        return;
    }

    const isDashboard = document.getElementById('emergencyDashboard') !== null;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(apiBase + '/hubs/incidents', {
            accessTokenFactory: getToken
        })
        .withAutomaticReconnect()
        .build();

    // ── Connection Lifecycle ────────────────────────────────────────────────
    connection.onreconnecting(function () {
        if (isDashboard) {
            setConnectionStatus('Reconnecting...', 'bg-warning text-dark');
        }
        if (window.resourceCommandCenter && typeof window.resourceCommandCenter.setConnectionStatus === 'function') {
            window.resourceCommandCenter.setConnectionStatus('Reconnecting...', 'bg-warning text-dark');
        }
        if (window.resourceRecommendations && typeof window.resourceRecommendations.setConnectionStatus === 'function') {
            window.resourceRecommendations.setConnectionStatus('Reconnecting...', 'bg-warning text-dark');
        }
    });

    connection.onreconnected(function () {
        if (isDashboard) {
            setConnectionStatus('Live', 'bg-success');
            refreshDashboard();
        }
        if (window.emergencyMapState && typeof window.emergencyMapState.refreshMap === 'function') {
            window.emergencyMapState.refreshMap();
        }
        if (window.resourceCommandCenter && typeof window.resourceCommandCenter.setConnectionStatus === 'function') {
            window.resourceCommandCenter.setConnectionStatus('Live', 'bg-success');
        }
        if (window.resourceCommandCenter && typeof window.resourceCommandCenter.refresh === 'function') {
            window.resourceCommandCenter.refresh();
        }
        if (window.resourceRecommendations && typeof window.resourceRecommendations.setConnectionStatus === 'function') {
            window.resourceRecommendations.setConnectionStatus('Live', 'bg-success');
        }
        if (window.resourceRecommendations && typeof window.resourceRecommendations.refresh === 'function') {
            window.resourceRecommendations.refresh();
        }
    });

    connection.onclose(function () {
        if (isDashboard) {
            setConnectionStatus('Disconnected', 'bg-danger');
        }
        if (window.resourceCommandCenter && typeof window.resourceCommandCenter.setConnectionStatus === 'function') {
            window.resourceCommandCenter.setConnectionStatus('Disconnected', 'bg-danger');
        }
        if (window.resourceRecommendations && typeof window.resourceRecommendations.setConnectionStatus === 'function') {
            window.resourceRecommendations.setConnectionStatus('Disconnected', 'bg-danger');
        }
    });

    // ── Dashboard & Map Live Update Listener ─────────────────────────────────
    connection.on('DashboardUpdated', async function (event) {
        console.log('[SignalR] Dashboard update received:', event);
        if (isDashboard) {
            await refreshDashboard();
        }
        if (window.emergencyMapState && typeof window.emergencyMapState.refreshMap === 'function') {
            await window.emergencyMapState.refreshMap();
        }
    });

    // ── Resource Live Update Listener ───────────────────────────────────────
    connection.on('ResourceUpdated', async function (event) {
        console.log('[SignalR] Resource update received:', event);
        if (window.emergencyMapState && typeof window.emergencyMapState.refreshMap === 'function') {
            await window.emergencyMapState.refreshMap();
        }
        if (isDashboard) {
            await refreshDashboard();
        }
        if (window.resourceCommandCenter && typeof window.resourceCommandCenter.refresh === 'function') {
            await window.resourceCommandCenter.refresh();
        }
        if (window.resourceRecommendations && typeof window.resourceRecommendations.refresh === 'function') {
            await window.resourceRecommendations.refresh();
        }
    });

    // ── Incident Details & Map Page Listener ─────────────────────────────────
    connection.on('IncidentUpdated', function (event) {
        console.debug('[SignalR] IncidentUpdated', event);

        if (window.emergencyMapState && typeof window.emergencyMapState.refreshMap === 'function') {
            window.emergencyMapState.refreshMap();
        }

        // Visual analysis badge update (Incident Details page)
        if (event.visualAnalysisAvailable && event.incidentMediaId) {
            const badge = document.querySelector(
                '[data-analysis-status="' + event.incidentMediaId + '"]'
            );
            if (badge) {
                const status = event.visualAnalysisStatus ?? 'Completed';
                badge.textContent = status;
                badge.className = 'badge ' + statusClass(status);
            }
        }

        // Auto-reload Incident Details when AI/visual pipeline finishes
        if (!isDashboard && (event.updateType === 'AI_ANALYSIS_COMPLETED' ||
            event.updateType === 'VISUAL_ANALYSIS_COMPLETED')) {
            const waiting = document.querySelector('[data-analysis-status]');
            if (waiting && waiting.classList.contains('bg-secondary')) {
                setTimeout(function () { location.reload(); }, 1500);
            }
        }
    });

    function statusClass(status) {
        switch (status) {
            case 'RequiresReview': return 'bg-warning text-dark';
            case 'Failed':        return 'bg-danger';
            case 'Rejected':      return 'bg-danger';
            default:              return 'bg-success';
        }
    }

    // ── Escalation Live Update Listener ─────────────────────────────────────
    connection.on('EscalationUpdated', async function (event) {
        console.log('[SignalR] Escalation update received:', event);
        if (isDashboard) {
            await refreshDashboard();
        }
        if (window.emergencyMapState && typeof window.emergencyMapState.refreshMap === 'function') {
            await window.emergencyMapState.refreshMap();
        }
        const currentContainer = document.querySelector('[data-incident-id]');
        if (currentContainer && currentContainer.dataset.incidentId === event.incidentId) {
            setTimeout(function () { location.reload(); }, 1200);
        }
    });

    // ── Situation Live Update Listener ───────────────────────────────────────
    connection.on('SituationUpdated', async function (event) {
        console.log('[SignalR] Situation update received:', event);

        if (typeof window.refreshSituationCommandCenter === 'function') {
            await window.refreshSituationCommandCenter();
        }

        if (typeof window.refreshSituationMap === 'function') {
            await window.refreshSituationMap();
        }

        if (typeof window.refreshDashboardSituationKpis === 'function') {
            await window.refreshDashboardSituationKpis();
        }

        if (isDashboard) {
            await refreshDashboard();
        }

        if (window.emergencyMapState && typeof window.emergencyMapState.refreshMap === 'function') {
            await window.emergencyMapState.refreshMap();
        }
    });

    // ── Notification Live Update Listener ────────────────────────────────────
    connection.on('NotificationCreated', async function (notification) {
        console.log('[SignalR] Notification created received:', notification);
        if (typeof window.showNotificationToast === 'function') {
            window.showNotificationToast(notification);
        }
        if (typeof window.loadNotificationSummary === 'function') {
            await window.loadNotificationSummary();
        }
        if (isDashboard) {
            await refreshDashboard();
        }
    });

    // ── Dashboard Updaters ──────────────────────────────────────────────────
    function setConnectionStatus(text, cssClass) {
        const element = document.getElementById('dashboardConnectionIndicator');
        if (!element) return;
        element.textContent = text;
        element.className = 'badge ' + cssClass;
    }

    function setText(id, value) {
        const element = document.getElementById(id);
        if (element) {
            element.textContent = value ?? 0;
        }
    }

    function updateDashboardSummary(summary) {
        if (!summary) return;

        const critical = summary.criticalIncidents ?? summary.CriticalIncidents ?? 0;
        const high = summary.highIncidents ?? summary.HighIncidents ?? 0;
        const active = summary.activeIncidents ?? summary.ActiveIncidents ?? 0;
        const unassigned = summary.unassignedIncidents ?? summary.UnassignedIncidents ?? 0;
        const aiReview = summary.awaitingAIReview ?? summary.AwaitingAIReview ?? 0;
        const dupReview = summary.awaitingDuplicateReview ?? summary.AwaitingDuplicateReview ?? 0;
        const visReview = summary.awaitingVisualReview ?? summary.AwaitingVisualReview ?? 0;

        const openEsc = summary.openEscalations ?? summary.OpenEscalations ?? 0;
        const critEsc = summary.criticalEscalations ?? summary.CriticalEscalations ?? 0;
        const unackEsc = summary.unacknowledgedEscalations ?? summary.UnacknowledgedEscalations ?? 0;

        const openSit = summary.openSituations ?? summary.OpenSituations ?? 0;
        const critSit = summary.criticalSituations ?? summary.CriticalSituations ?? 0;
        const shortSit = summary.situationsWithResourceShortage ?? summary.SituationsWithResourceShortage ?? 0;

        const unreadNotif = summary.unreadNotifications ?? summary.UnreadNotifications ?? 0;
        const critNotif = summary.criticalNotifications ?? summary.CriticalNotifications ?? 0;
        const urgNotif = summary.urgentNotifications ?? summary.UrgentNotifications ?? 0;

        setText('criticalIncidentCount', critical);
        setText('highIncidentCount', high);
        setText('activeIncidentCount', active);
        setText('unassignedIncidentCount', unassigned);

        setText('aiReviewCount', aiReview);
        setText('duplicateReviewCount', dupReview);
        setText('visualReviewCount', visReview);

        setText('openEscalationsCount', openEsc);
        setText('criticalEscalationsCount', critEsc);
        setText('unacknowledgedEscalationsCount', unackEsc);

        setText('openSituationsCount', openSit);
        setText('criticalSituationsCount', critSit);
        setText('situationsWithResourceShortageCount', shortSit);

        setText('unreadNotificationsCount', unreadNotif);
        setText('criticalNotificationsCount', critNotif);
        setText('urgentNotificationsCount', urgNotif);

        setText('notesActiveCount', active);
        setText('notesUnassignedCount', unassigned);
        setText('notesPendingReviewCount', aiReview + dupReview + visReview);
    }

    function updateIncidentGrid(incidents) {
        const element = document.getElementById('incidentGrid');
        const grid = element?.ej2_instances?.[0];
        if (!grid || !Array.isArray(incidents)) return;

        const normalized = incidents.map(function (item) {
            return {
                ...item,
                Id: item.Id ?? item.id,
                IncidentNumber: item.IncidentNumber ?? item.incidentNumber,
                IncidentType: item.IncidentType ?? item.incidentType,
                Title: item.Title ?? item.title,
                Severity: item.Severity ?? item.severity,
                Priority: item.Priority ?? item.priority,
                Status: item.Status ?? item.status,
                CreatedAtUtc: item.CreatedAtUtc ?? item.createdAtUtc,
                IsAssigned: item.IsAssigned ?? item.isAssigned,
                RequiresAIReview: item.RequiresAIReview ?? item.requiresAIReview,
                HasDuplicateCandidates: item.HasDuplicateCandidates ?? item.hasDuplicateCandidates,
                HasVisualReview: item.HasVisualReview ?? item.hasVisualReview,
                DetailsUrl: item.DetailsUrl ?? item.detailsUrl
            };
        });

        grid.dataSource = normalized;
    }

    function updateSeverityChart(data) {
        const element = document.getElementById('severityChart');
        const chart = element?.ej2_instances?.[0];
        if (!chart || !Array.isArray(data)) return;

        const normalized = data.map(function (item) {
            return {
                ...item,
                Label: item.Label ?? item.label,
                Value: item.Value ?? item.value
            };
        });

        chart.series[0].dataSource = normalized;
        chart.refresh();
    }

    function updateIncidentTrendChart(data) {
        const element = document.getElementById('incidentTrendChart');
        const chart = element?.ej2_instances?.[0];
        if (!chart || !Array.isArray(data)) return;

        const normalized = data.map(function (item) {
            return {
                ...item,
                Date: item.Date ?? item.date,
                Count: item.Count ?? item.count
            };
        });

        chart.series[0].dataSource = normalized;
        chart.refresh();
    }

    function updateReviewQueueChart(data) {
        const element = document.getElementById('reviewQueueChart');
        const chart = element?.ej2_instances?.[0];
        if (!chart || !Array.isArray(data)) return;

        const normalized = data.map(function (item) {
            return {
                ...item,
                Category: item.Category ?? item.category,
                Count: item.Count ?? item.count
            };
        });

        chart.series[0].dataSource = normalized;
        chart.refresh();
    }

    function updateDashboardTimestamp(updatedAtUtc) {
        const element = document.getElementById('dashboardLastUpdated');
        if (!element) return;
        const timestamp = updatedAtUtc ? new Date(updatedAtUtc) : new Date();
        element.textContent = 'Updated ' + timestamp.toLocaleTimeString();
    }

    let _refreshDebounce = null;
    async function refreshDashboard() {
        if (!isDashboard) return;

        clearTimeout(_refreshDebounce);
        _refreshDebounce = setTimeout(async function () {
            try {
                const response = await fetch('/Dashboard/Snapshot', {
                    method: 'GET',
                    credentials: 'same-origin',
                    headers: {
                        'Accept': 'application/json'
                    }
                });

                if (!response.ok) {
                    throw new Error('Dashboard request failed: ' + response.status);
                }

                const data = await response.json();

                updateDashboardSummary(data.summary);
                updateIncidentGrid(data.incidents);
                updateSeverityChart(data.severityDistribution);
                updateIncidentTrendChart(data.incidentTrend);
                updateReviewQueueChart(data.reviewQueue);
                updateDashboardTimestamp(data.updatedAtUtc);
            }
            catch (error) {
                console.error('[SignalR] Dashboard refresh failed:', error);
            }
        }, 150);
    }

    // ── Start Realtime ───────────────────────────────────────────────────────
    async function startRealtime() {
        try {
            await connection.start();

            if (isDashboard) {
                setConnectionStatus('Live', 'bg-success');
                await refreshDashboard();
                console.log('[SignalR] Dashboard SignalR connected & initialized.');
            } else {
                console.log('[SignalR] Incident realtime connected.');
            }

            if (window.resourceCommandCenter && typeof window.resourceCommandCenter.setConnectionStatus === 'function') {
                window.resourceCommandCenter.setConnectionStatus('Live', 'bg-success');
            }
            if (window.resourceRecommendations && typeof window.resourceRecommendations.setConnectionStatus === 'function') {
                window.resourceRecommendations.setConnectionStatus('Live', 'bg-success');
            }
        }
        catch (error) {
            console.error('[SignalR] Connection failed:', error);
            if (isDashboard) {
                setConnectionStatus('Connection failed', 'bg-danger');
            }
            if (window.resourceCommandCenter && typeof window.resourceCommandCenter.setConnectionStatus === 'function') {
                window.resourceCommandCenter.setConnectionStatus('Connection failed', 'bg-danger');
            }
            if (window.resourceRecommendations && typeof window.resourceRecommendations.setConnectionStatus === 'function') {
                window.resourceRecommendations.setConnectionStatus('Connection failed', 'bg-danger');
            }
        }
    }

    startRealtime();
}());
