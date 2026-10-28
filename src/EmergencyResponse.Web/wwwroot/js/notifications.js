/**
 * Emergency Response - Operational Notifications & Real-Time Alerting
 */
(function () {
    function getAntiCsrfToken() {
        const input = document.querySelector('input[name="__RequestVerificationToken"]');
        return input ? input.value : '';
    }

    function timeAgo(dateString) {
        if (!dateString) return '';
        const utcDate = new Date(dateString.endsWith('Z') ? dateString : dateString + 'Z');
        const seconds = Math.floor((new Date() - utcDate) / 1000);
        if (seconds < 5) return 'just now';
        if (seconds < 60) return seconds + 's ago';
        const minutes = Math.floor(seconds / 60);
        if (minutes < 60) return minutes + 'm ago';
        const hours = Math.floor(minutes / 60);
        if (hours < 24) return hours + 'h ago';
        const days = Math.floor(hours / 24);
        return days + 'd ago';
    }

    function updateBadge(totalUnread) {
        const badge = document.getElementById('notificationBadge');
        if (!badge) return;
        const bell = document.getElementById('notificationBell');
        bell?.setAttribute('aria-label', totalUnread > 0 ? `Notifications, ${totalUnread} unread` : 'Notifications, no unread alerts');

        if (totalUnread && totalUnread > 0) {
            badge.textContent = totalUnread > 99 ? '99+' : totalUnread;
            badge.classList.remove('d-none');
        } else {
            badge.classList.add('d-none');
        }
    }

    async function loadSummary() {
        try {
            const resp = await fetch('/Notifications/Summary', {
                headers: { 'Accept': 'application/json' },
                credentials: 'same-origin'
            });
            const contentType = resp.headers?.get?.('content-type') || '';
            if (resp.redirected || (contentType && !contentType.includes('application/json'))) {
                document.getElementById('notificationBell')?.setAttribute('aria-label', 'Notifications, unread count unavailable');
                return;
            }
            if (!resp.ok) throw new Error('Notification summary unavailable');
            const data = await resp.json();
            updateBadge(data.totalUnread);
        } catch (err) {
            document.getElementById('notificationBell')?.setAttribute('aria-label', 'Notifications, unread count unavailable');
            console.warn('Failed to load notification summary:', err);
        }
    }

    async function loadDropdown() {
        const list = document.getElementById('notificationList');
        if (!list) return;
        list.setAttribute('aria-busy', 'true');

        try {
            const resp = await fetch('/Notifications/Snapshot?take=7');
            if (!resp.ok) throw new Error('Notification list unavailable');
            const items = await resp.json();
            if (!Array.isArray(items)) throw new Error('Invalid notification list');

            if (!items || items.length === 0) {
                list.innerHTML = '<div class="text-center text-muted p-3 small">No notifications</div>';
                return;
            }

            let html = '';
            for (const item of items) {
                const priorityBadgeClass = item.priority === 4 ? 'bg-danger text-white' :
                                           item.priority === 3 ? 'bg-warning text-dark' :
                                           item.priority === 2 ? 'bg-info text-dark' : 'bg-secondary text-white';
                const bgClass = item.isRead ? 'bg-white' : 'bg-light';
                const url = item.actionUrl || (item.incidentId ? `/Incidents/Details/${item.incidentId}` :
                            (item.operationalSituationId ? `/Situations/Details/${item.operationalSituationId}` : '/Dashboard'));

                html += `
                    <div class="p-2 border-bottom ${bgClass} notification-dropdown-item" id="notif-drop-${item.id}">
                        <div class="d-flex justify-content-between align-items-start gap-1 mb-1">
                            <span class="badge ${priorityBadgeClass}" style="font-size:0.65rem;">${item.priorityName}</span>
                            <span class="text-muted" style="font-size:0.7rem;">${timeAgo(item.createdAtUtc)}</span>
                        </div>
                        <div class="small fw-bold text-dark text-truncate mb-1" title="${escapeHtml(item.title)}">${escapeHtml(item.title)}</div>
                        <div class="text-muted small text-truncate mb-2" style="font-size:0.75rem;" title="${escapeHtml(item.message)}">${escapeHtml(item.message)}</div>
                        <div class="d-flex justify-content-between align-items-center">
                            <a href="${url}" class="btn btn-xs btn-outline-primary py-0 px-1" style="font-size:0.7rem;">View</a>
                            ${!item.isRead ? `<button class="btn btn-xs btn-link text-muted p-0 text-decoration-none" style="font-size:0.7rem;" onclick="markNotificationRead('${item.id}', event)">Mark read</button>` : ''}
                        </div>
                    </div>
                `;
            }
            list.innerHTML = html;
        } catch (err) {
            list.innerHTML = '<div class="text-center text-danger p-3 small">Error loading alerts</div>';
        }
        finally { list.setAttribute('aria-busy', 'false'); }
    }

    function showToast(notification) {
        const container = document.getElementById('notificationToastContainer');
        if (!container) return;

        const priorityBadgeClass = notification.priority === 4 ? 'bg-danger text-white' :
                                   notification.priority === 3 ? 'bg-warning text-dark' :
                                   notification.priority === 2 ? 'bg-info text-dark' : 'bg-secondary text-white';

        const toastId = 'toast-' + Math.random().toString(36).substring(2, 9);
        const url = notification.actionUrl || (notification.incidentId ? `/Incidents/Details/${notification.incidentId}` :
                    (notification.operationalSituationId ? `/Situations/Details/${notification.operationalSituationId}` : '/Dashboard'));

        const actionText = notification.incidentId ? 'View Incident' :
                           notification.operationalSituationId ? 'View Situation' : 'View Details';

        const toastHtml = `
            <div id="${toastId}" class="toast shadow-lg border-start border-4 ${notification.priority === 4 ? 'border-danger' : (notification.priority === 3 ? 'border-warning' : 'border-primary')}" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="toast-header">
                    <span class="badge ${priorityBadgeClass} me-2">${notification.priorityName || 'Alert'}</span>
                    <strong class="me-auto text-dark small text-truncate" style="max-width: 180px;">${escapeHtml(notification.title)}</strong>
                    <small class="text-muted">just now</small>
                    <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body bg-white">
                    <p class="small text-muted mb-2">${escapeHtml(notification.message)}</p>
                    <div class="d-flex justify-content-end gap-2">
                        <a href="${url}" class="btn btn-sm btn-primary py-1 px-2" style="font-size:0.75rem;">${actionText} &rarr;</a>
                    </div>
                </div>
            </div>
        `;

        container.insertAdjacentHTML('beforeend', toastHtml);
        const toastEl = document.getElementById(toastId);
        if (toastEl && window.bootstrap && typeof window.bootstrap.Toast === 'function') {
            const toast = new bootstrap.Toast(toastEl, { delay: 10000 });
            toast.show();
            toastEl.addEventListener('hidden.bs.toast', function () {
                toastEl.remove();
            });
        }
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

    // Global hooks
    window.loadNotificationSummary = loadSummary;
    window.showNotificationToast = function (notification) {
        showToast(notification);
        loadSummary();
        loadDropdown();
    };

    window.markNotificationRead = async function (id, event) {
        if (event) event.stopPropagation();
        try {
            const resp = await fetch(`/Notifications/MarkRead/${id}`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiCsrfToken()
                }
            });
            if (resp.ok) {
                loadSummary();
                loadDropdown();
                const pageRow = document.getElementById(`notif-row-${id}`);
                if (pageRow) {
                    pageRow.classList.remove('fw-bold');
                    pageRow.classList.add('opacity-75');
                    const btn = pageRow.querySelector('.btn-mark-read');
                    if (btn) btn.remove();
                }
            }
        } catch (err) {
            console.error('Failed to mark notification as read:', err);
        }
    };

    window.markAllNotificationsRead = async function (event) {
        if (event) event.stopPropagation();
        try {
            const resp = await fetch('/Notifications/MarkAllRead', {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': getAntiCsrfToken()
                }
            });
            if (resp.ok) {
                loadSummary();
                loadDropdown();
                if (window.location.pathname.toLowerCase().includes('/notifications')) {
                    window.location.reload();
                }
            }
        } catch (err) {
            console.error('Failed to mark all as read:', err);
        }
    };

    document.addEventListener('DOMContentLoaded', function () {
        loadSummary();

        const bell = document.getElementById('notificationBell');
        if (bell) {
            bell.addEventListener('show.bs.dropdown', function () {
                loadDropdown();
            });
        }
    });
})();
