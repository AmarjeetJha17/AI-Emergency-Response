// Dependency-free logic/DOM-contract checks. These do not verify browser layout.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');
const root = path.resolve(__dirname, '../..');
const web = path.join(root, 'src/EmergencyResponse.Web');
const { describeOutlook } = require(path.join(web, 'wwwroot/js/dashboard-outlook.js'));
const valid = { isAvailable: true, generatedAtUtc: '2026-10-15T00:00:00Z', points: [{ forecast: 2 }, { forecast: 3.2 }] };
test('real values remain advisory and shortages are not fabricated', () => {
    const result = describeOutlook(valid, [{ trend: 'Increasing' }, { trend: 'Stable' }]);
    assert.equal(result.volume, '5'); assert.equal(result.hotspots, '1');
    assert.equal(result.shortage, 'Not assessed'); assert.equal(result.available, true);
});
for (const [name, volume, hotspots] of [
    ['provider unavailable', { ...valid, isAvailable: false }, []],
    ['missing availability', { points: valid.points }, []],
    ['empty points', { ...valid, points: [] }, []],
    ['invalid number', { ...valid, points: [{ forecast: NaN }] }, []],
    ['string number', { ...valid, points: [{ forecast: '3' }] }, []],
    ['negative forecast', { ...valid, points: [{ forecast: -1 }] }, []],
    ['null point', { ...valid, points: [null] }, []],
    ['missing generation time', { ...valid, generatedAtUtc: null }, []]
]) test(name + ' cannot produce a false zero or success timestamp', () => {
    const result = describeOutlook(volume, hotspots);
    assert.equal(result.available, false); assert.equal(result.volume, 'Unavailable');
    assert.doesNotMatch(result.status, /^Updated|^Advisory forecast generated/);
});
test('verified zero remains distinct from unavailable', () => {
    assert.equal(describeOutlook({ ...valid, points: [{ forecast: 0 }] }, []).volume, '0');
});
test('unavailable forecast does not hide a valid report-based hotspot trend', () => {
    const result = describeOutlook({ ...valid, isAvailable: false }, [{ trend: 'Increasing' }]);
    assert.equal(result.available, false); assert.equal(result.volume, 'Unavailable');
    assert.equal(result.hotspots, '1'); assert.match(result.status, /not a model forecast/);
});
test('missing hotspot data never becomes zero and does not hide a valid forecast', () => {
    const result = describeOutlook(valid, {});
    assert.equal(result.available, false); assert.equal(result.hotspots, 'Unavailable');
    assert.equal(result.volume, '5'); assert.match(result.status, /Hotspot data is unavailable/);
});
function fixture(file, fetch) {
    const elements = new Map();
    const get = id => {
        if (!elements.has(id)) elements.set(id, { textContent: '', dataset: {}, attributes: {}, disabled: false,
            callbacks: {}, classList: { toggle() {}, add() {}, remove() {} },
            setAttribute(key, value) { this.attributes[key] = value; },
            addEventListener(key, callback) { this.callbacks[key] = callback; } });
        return elements.get(id);
    };
    const timers = [];
    const events = {};
    const document = { getElementById: get, currentScript: { dataset: {} }, querySelector: () => null,
        addEventListener: (name, fn) => events[name] = fn };
    class Hub {
        on() {} onreconnecting() {} onreconnected() {} onclose() {} start() { return Promise.resolve(); }
    }
    class Builder { withUrl() { return this; } withAutomaticReconnect() { return this; } build() { return new Hub(); } }
    const context = { document, fetch, console: { log() {}, error() {}, warn() {} },
        signalR: { HubConnectionBuilder: Builder }, matchMedia: () => ({ matches: true }),
        setTimeout: fn => { timers.push(fn); return timers.length; }, clearTimeout() {} };
    context.window = context;
    vm.runInNewContext(fs.readFileSync(path.join(web, 'wwwroot/js', file), 'utf8'), context);
    return { get, timers, events, context };
}
const tick = () => new Promise(resolve => setImmediate(resolve));
const snapshot = { summary: Object.fromEntries(['criticalIncidents','highIncidents','activeIncidents','unassignedIncidents',
    'awaitingAIReview','awaitingDuplicateReview','awaitingVisualReview','openEscalations','criticalEscalations',
    'unacknowledgedEscalations','openSituations','criticalSituations','situationsWithResourceShortage',
    'unreadNotifications','criticalNotifications','urgentNotifications'].map(key => [key, 0])),
    incidents: [{ priority: 'High' }, { priority: 'Critical' }, { priority: 'Normal' }],
    severityDistribution: [], incidentTrend: [], reviewQueue: [], updatedAtUtc: '2026-10-15T00:00:00Z' };
test('valid snapshot updates high priority separately from high severity', async () => {
    const f = fixture('incident-realtime.js', async () => ({ ok: true, json: async () => snapshot }));
    await tick(); await f.timers.shift()();
    assert.equal(f.get('highPriorityIncidentCount').textContent, 1);
    assert.equal(f.get('highIncidentCount').textContent, 0);
    assert.equal(f.get('dashboardRefreshStatus').dataset.state, 'current');
});
for (const [name, response] of [['failed HTTP', { ok: false, status: 500 }],
    ['invalid snapshot', { ok: true, json: async () => ({ ...snapshot, summary: {} }) }]]) {
    test(name + ' retains prior counts and labels them stale', async () => {
        const f = fixture('incident-realtime.js', async () => response);
        f.get('activeIncidentCount').textContent = '6';
        f.get('dashboardLastUpdated').textContent = 'Old retrieval';
        await tick(); await f.timers.shift()();
        assert.equal(f.get('activeIncidentCount').textContent, '6');
        assert.equal(f.get('dashboardLastUpdated').textContent, 'Old retrieval');
        assert.equal(f.get('dashboardRefreshStatus').dataset.state, 'failed');
        assert.equal(f.get('refreshDashboardButton').disabled, false);
    });
}
test('failed forecast refresh clears previously displayed values', async () => {
    const f = fixture('dashboard-outlook.js', async () => ({ ok: false }));
    f.get('forecastIncidentVolume').textContent = '123';
    f.events.DOMContentLoaded(); await tick();
    assert.equal(f.get('forecastIncidentVolume').textContent, 'Unavailable');
    assert.equal(f.get('forecastShortageCount').textContent, 'Not assessed');
    assert.equal(f.get('refreshPredictiveOutlook').disabled, false);
});
test('notification bell announces unread count and HTTP failure is explicit', async () => {
    let failed = false;
    const f = fixture('notifications.js', async () => ({ ok: !failed, json: async () => ({ totalUnread: 3 }) }));
    f.events.DOMContentLoaded(); await tick();
    assert.equal(f.get('notificationBell').attributes['aria-label'], 'Notifications, 3 unread');
    failed = true;
    f.get('notificationBell').callbacks['show.bs.dropdown'](); await tick();
    assert.match(f.get('notificationList').innerHTML, /Error loading alerts/);
    assert.equal(f.get('notificationList').attributes['aria-busy'], 'false');
});
test('all legacy dashboard hooks and Syncfusion components are present', () => {
    const view = fs.readFileSync(path.join(web, 'Views/Dashboard/Index.cshtml'), 'utf8');
    for (const id of ['emergencyDashboard','dashboardConnectionIndicator','dashboardLastUpdated','criticalIncidentCount',
        'highIncidentCount','activeIncidentCount','unassignedIncidentCount','aiReviewCount','duplicateReviewCount',
        'visualReviewCount','openEscalationsCount','criticalEscalationsCount','unacknowledgedEscalationsCount',
        'openSituationsCount','criticalSituationsCount','situationsWithResourceShortageCount','unreadNotificationsCount',
        'criticalNotificationsCount','urgentNotificationsCount','incidentGrid','severityChart','incidentTrendChart',
        'reviewQueueChart','notesActiveCount','notesUnassignedCount','notesPendingReviewCount','refreshPredictiveOutlook',
        'predictiveOutlookStatus','forecastIncidentVolume','forecastShortageCount','forecastHotspotCount',
        'severityTemplate','priorityTemplate','alertTemplate','incidentActions']) {
        assert.equal((view.match(new RegExp('id="' + id + '"', 'g')) || []).length, 1, id);
    }
    assert.match(view, /allowPaging="true"/); assert.match(view, /showColumnChooser="true"/);
});
test('map selection is restored with reduced motion respected', () => {
    const f = fixture('dashboard-outlook.js', async () => ({ ok: false }));
    f.get('emergencyDashboard').dataset.selectedIncidentId = 'ABC';
    let selected = null, scrolling = null;
    f.get('incidentGrid').ej2_instances = [{ currentViewData: [{ Id: 'def' }, { Id: 'abc' }],
        selectRow: index => selected = index, getRowByIndex: () => ({ scrollIntoView: args => scrolling = args }) }];
    f.context.focusSelectedIncident();
    assert.equal(selected, 1); assert.equal(scrolling.behavior, 'auto');
});
test('refresh preserves the grid instance and binds new authoritative rows', async () => {
    const f = fixture('incident-realtime.js', async () => ({ ok: true, json: async () => snapshot }));
    const grid = { dataSource: [] };
    f.get('incidentGrid').ej2_instances = [grid];
    await tick(); await f.timers.shift()();
    assert.equal(f.get('incidentGrid').ej2_instances[0], grid);
    assert.equal(grid.dataSource.length, 3); assert.equal(grid.dataSource[0].Priority, 'High');
});
test('shared shell preserves CSRF, script ordering and accessible navigation targets', () => {
    const shell = fs.readFileSync(path.join(web, 'Views/Shared/_Layout.cshtml'), 'utf8');
    assert.match(shell, /data-bs-target="#primaryNavigation" aria-controls="primaryNavigation"/);
    assert.match(shell, /id="primaryNavigation"/); assert.match(shell, /@Html.AntiForgeryToken\(\)/);
    const scripts = ['<ejs-scripts>', 'jquery.min.js', 'bootstrap.bundle.min.js', 'site.js', 'signalr.js', 'incident-realtime.js', 'notifications.js', 'RenderSectionAsync("Scripts"'];
    for (let index = 1; index < scripts.length; index++) {
        assert.ok(shell.indexOf(scripts[index - 1]) < shell.indexOf(scripts[index]), scripts[index]);
    }
});
test('design token foreground/background pairs meet WCAG AA normal-text contrast', () => {
    const css = fs.readFileSync(path.join(web, 'wwwroot/css/site.css'), 'utf8');
    function token(name) {
        const value = css.match(new RegExp('--er-' + name + ': (#([a-f0-9]{3}|[a-f0-9]{6}));'))[1].slice(1);
        return value.length === 3 ? value.split('').map(c => c + c).join('') : value;
    }
    function luminance(hex) {
        const rgb = [0, 2, 4].map(offset => parseInt(hex.slice(offset, offset + 2), 16) / 255)
            .map(v => v <= .04045 ? v / 12.92 : ((v + .055) / 1.055) ** 2.4);
        return rgb[0] * .2126 + rgb[1] * .7152 + rgb[2] * .0722;
    }
    for (const [foreground, background] of [['text','surface'], ['text-secondary','canvas'], ['brand','surface'],
        ['critical','critical-soft'], ['high','high-soft'], ['moderate','moderate-soft'], ['low','low-soft']]) {
        const a = luminance(token(foreground)), b = luminance(token(background));
        assert.ok((Math.max(a, b) + .05) / (Math.min(a, b) + .05) >= 4.5, foreground);
    }
});
