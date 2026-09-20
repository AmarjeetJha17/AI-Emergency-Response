/* incident-details.js
 * Client-side helpers for the Incident Details page.
 * Loaded as a static file to avoid Razor interfering with JS template literals.
 */

function openEvidence(imageUrl) {
    var el = document.getElementById('evidenceDialog');
    var dialog = el && el.ej2_instances && el.ej2_instances[0];

    if (!dialog) {
        // Syncfusion dialog not yet initialised — open image in new tab as fallback.
        window.open(imageUrl, '_blank');
        return;
    }

    dialog.content =
        '<div class="h-100 d-flex align-items-center justify-content-center">' +
        '<img src="' + imageUrl + '" alt="Emergency evidence" ' +
        'style="max-width:100%; max-height:100%; object-fit:contain;" />' +
        '</div>';
    dialog.show();
}

function openFrameFindings(incidentId, mediaId, json) {
    var frames = JSON.parse(json);
    var el = document.getElementById('frameDialog');
    var dialog = el && el.ej2_instances && el.ej2_instances[0];

    var rows = frames.map(function (f) {
        var mm = String(Math.floor(f.timestampSeconds / 60)).padStart(2, '0');
        var ss = String(Math.floor(f.timestampSeconds % 60)).padStart(2, '0');

        var hazards = (f.detectedHazards || []).length
            ? f.detectedHazards
                .map(function (h) {
                    return '<span class="badge bg-danger me-1">' + h + '</span>';
                })
                .join('')
            : '<span class="text-muted">None</span>';

        // Frame image proxied through the Web MVC action so the bearer
        // token is applied automatically by BearerTokenHandler.
        var frameUrl =
            '/Incidents/Frame?incidentId=' + incidentId +
            '&mediaId=' + mediaId +
            '&frameNumber=' + f.frameNumber;

        return '<div class="border rounded p-3 mb-3">' +
            '<div class="fw-bold mb-2">' + mm + ':' + ss + '</div>' +
            '<div class="mb-2 text-center bg-dark rounded overflow-hidden" style="max-height:220px">' +
            '<img src="' + frameUrl + '" alt="Frame at ' + mm + ':' + ss + '" ' +
            'class="img-fluid" style="max-height:220px;object-fit:contain;" ' +
            'onerror="this.closest(\'.mb-2\').style.display=\'none\'" />' +
            '</div>' +
            '<p class="mb-1 small">' + (f.sceneDescription || '') + '</p>' +
            '<div class="mb-1"><span class="small text-muted">Hazards: </span>' + hazards + '</div>' +
            '<div class="row g-1 small mt-1">' +
            '<div class="col-6">Flooding: ' + (f.floodingVisible ? 'Yes' : 'No') + '</div>' +
            '<div class="col-6">Road blocked: ' + (f.roadAppearsBlocked ? 'Yes' : 'No') + '</div>' +
            '<div class="col-6">Fire: ' + (f.fireVisible ? 'Yes' : 'No') + '</div>' +
            '<div class="col-6">Smoke: ' + (f.smokeVisible ? 'Yes' : 'No') + '</div>' +
            '<div class="col-6">Immediate danger: ' + (f.immediateDangerVisible ? 'Yes' : 'No') + '</div>' +
            '<div class="col-6">People trapped: ' + (f.peopleAppearTrapped ? 'Yes' : 'No') + '</div>' +
            '</div>' +
            '<div class="small text-muted mt-1">Confidence: ' +
            (f.confidence * 100).toFixed(0) + '%</div>' +
            '</div>';
    }).join('');

    if (!dialog) {
        // Syncfusion dialog not yet initialised — alert as fallback.
        alert('Frame findings dialog is not ready. Please wait for the page to fully load and try again.');
        return;
    }

    dialog.content =
        '<div style="overflow-y:auto;max-height:80vh;padding:1rem">' + rows + '</div>';
    dialog.show();
}
