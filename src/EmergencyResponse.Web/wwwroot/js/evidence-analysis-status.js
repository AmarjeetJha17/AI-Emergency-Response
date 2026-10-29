/* Only the upload response can confirm whether analysis was queued. */
function evidenceUploadStatus(result) {
    if (result && result.analysisStatus === 'Queued') {
        return 'Upload successful — AI analysis queued. Results require review.';
    }
    if (result && result.analysisStatus === 'Disabled') {
        return 'Upload successful — evidence stored. AI analysis is disabled; no analysis was queued.';
    }
    return 'Upload successful — analysis status is unconfirmed.';
}
if (typeof module !== 'undefined' && module.exports) module.exports = { evidenceUploadStatus };
