(function () {
    const apiBase = document.currentScript?.dataset.apiBase ?? '';

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(apiBase + '/hubs/incident')
        .withAutomaticReconnect()
        .build();

    connection.on('IncidentUpdated', function (event) {
        if (!event.visualAnalysisAvailable) return;

        const badge = document.querySelector(
            `[data-analysis-status="${event.incidentMediaId ?? ''}"]`
        );

        if (!badge) return;

        const status = event.visualAnalysisStatus ?? 'Completed';

        badge.textContent = status;
        badge.className = 'badge ' + statusClass(status);
    });

    function statusClass(status) {
        switch (status) {
            case 'RequiresReview': return 'bg-warning text-dark';
            case 'Failed':        return 'bg-danger';
            case 'Rejected':      return 'bg-danger';
            default:              return 'bg-success';
        }
    }

    connection.start().catch(function (err) {
        console.warn('SignalR connection failed:', err);
    });
}());
