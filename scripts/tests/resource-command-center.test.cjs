const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

const root = path.resolve(__dirname, '../..');
const viewPath = path.join(root, 'src/EmergencyResponse.Web/Views/Resources/Index.cshtml');
const view = fs.readFileSync(viewPath, 'utf8');
const inlineScript = view.match(/@section Scripts\s*\{\s*<script>([\s\S]*?)<\/script>/)?.[1];
assert.ok(inlineScript, 'resource command center script exists');

function fixture(fetch) {
    const elements = new Map();
    const get = id => {
        if (!elements.has(id)) elements.set(id, { textContent: '', dataset: {}, classList: { add() {}, remove() {} },
            callbacks: {}, addEventListener(name, callback) { this.callbacks[name] = callback; } });
        return elements.get(id);
    };
    let ready;
    const document = { getElementById: get, querySelector: () => null,
        addEventListener: (name, callback) => { if (name === 'DOMContentLoaded') ready = callback; } };
    const context = { document, fetch, console: { error() {} }, window: {}, Date };
    context.window = context;
    vm.runInNewContext(inlineScript, context);
    ready();
    return { get, context };
}

test('resource page does not claim a retrieval before the first successful snapshot', () => {
    const f = fixture(async () => ({ ok: false, status: 503 }));
    assert.equal(f.get('resourceLastUpdated').textContent, '');
});

test('failed resource snapshot reports stale state and keeps the last retrieval time', async () => {
    const f = fixture(async () => ({ ok: false, status: 503 }));
    f.get('resourceLastUpdated').textContent = 'Updated 10:00 UTC';
    await f.context.resourceCommandCenter.refresh();
    assert.equal(f.get('resourceLastUpdated').textContent, 'Updated 10:00 UTC');
    assert.equal(f.get('resourceRefreshStatus').dataset.state, 'failed');
});

test('resource grid templates do not interpolate untrusted values into HTML or inline scripts', () => {
    const templates = view.slice(view.indexOf('<script id="resourceStatusTemplate"'), view.indexOf('@section Scripts'));
    assert.doesNotMatch(templates, /\$\{(?:Status|ActiveIncidentTitle|ActiveIncidentNumber|Name|DepartmentName)\}/);
    assert.doesNotMatch(templates, /onclick="/);
    assert.match(templates, /escapeResourceHtml\(ActiveIncidentTitle\)/);
    assert.match(templates, /data-resource-name="\$\{escapeResourceHtml\(Name\)\}"/);
});

test('resource template encoder neutralizes HTML and attribute delimiters', () => {
    const helper = view.match(/<script>\s*(function escapeResourceHtml[\s\S]*?)<\/script>/)?.[1];
    assert.ok(helper);
    const context = {};
    vm.runInNewContext(helper + '\nthis.encode = escapeResourceHtml;', context);
    assert.equal(context.encode(`"><img src=x onerror='alert(1)'>&`), '&quot;&gt;&lt;img src=x onerror=&#39;alert(1)&#39;&gt;&amp;');
});

test('resource action errors render API messages as text', () => {
    assert.match(view, /alert\.textContent = message/);
    assert.doesNotMatch(view, /innerHTML\s*=\s*['"][^\n]*result\.message/);
});

test('map snapshot errors preserve the last timestamp and report stale data', () => {
    const mapScript = fs.readFileSync(path.join(root, 'src/EmergencyResponse.Web/wwwroot/js/emergency-map.js'), 'utf8');
    assert.match(mapScript, /setMapRefreshStatus\('failed', 'Refresh failed\. Showing the last retrieved snapshot\.'/);
    assert.match(mapScript, /new Date\(data\.updatedAtUtc\)/);
    assert.doesNotMatch(mapScript, /new Date\(\)\.toLocaleTimeString\(\)/);
});
