const test = require('node:test');
const assert = require('node:assert/strict');
const { evidenceUploadStatus } = require('../../src/EmergencyResponse.Web/wwwroot/js/evidence-analysis-status.js');

test('disabled analysis response never claims analysis was queued', () => {
    const text = evidenceUploadStatus({ analysisStatus: 'Disabled' });
    assert.match(text, /evidence stored/);
    assert.match(text, /no analysis was queued/);
    assert.doesNotMatch(text, /AI analysis queued/);
});
test('only a confirmed Queued response claims queueing', () => {
    assert.match(evidenceUploadStatus({ analysisStatus: 'Queued' }), /AI analysis queued/);
});
test('missing, malformed or unexpected analysis status stays unconfirmed', () => {
    for (const value of [null, undefined, {}, { analysisStatus: 'Completed' }, { analysisStatus: '<secret>' }]) {
        assert.equal(evidenceUploadStatus(value), 'Upload successful — analysis status is unconfirmed.');
    }
});
