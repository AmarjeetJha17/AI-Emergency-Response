"use strict";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7191/hubs/incidents", {
        accessTokenFactory: () =>
            window.emergencyResponseAccessToken ?? ""
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Information)
    .build();

connection.on("IncidentUpdated", function (notification) {
    console.log("Incident updated:", notification);

    window.dispatchEvent(
        new CustomEvent(
            "incidentUpdated",
            {
                detail: notification
            }));
});

async function startIncidentHub() {
    try {
        await connection.start();

        console.log("Incident SignalR connected.");
    }
    catch (err) {
        console.error("SignalR connection failed.", err);

        setTimeout(startIncidentHub, 5000);
    }
}

startIncidentHub();