# Data Classification

Classify information according to the harm caused by unauthorized access or disclosure. Classification supports handling decisions; it does not replace authorization, encryption, or data minimization.

| Classification | Meaning | Typical handling |
| --- | --- | --- |
| Public | Approved for unrestricted public release | May be shared publicly; preserve source and integrity |
| Internal | Routine operational information for authorized personnel | Authenticated access; do not publish without review |
| Confidential | Incident or operational information whose disclosure could cause harm | Need-to-know access, protected transport and storage, minimized copies |
| Restricted | Identity, authentication, secrets, and high-impact security records | Strong access controls, minimal collection, strict logging and export controls |

## Initial project mapping

| Data | Classification |
| --- | --- |
| Public drug and disaster information | Public |
| General incident metadata | Internal |
| Incident descriptions and exact locations | Confidential |
| Uploaded photos and videos | Confidential |
| Citizen contact and operator identity information | Restricted |
| Authentication and session data | Restricted |
| AI prompts or outputs containing operational data | Confidential |
| Copilot conversations and action proposals | Confidential |
| Knowledge documents | Internal by default; Confidential when content warrants it |
| Resource and situation operational information | Confidential |
| Audit records and credentials/secrets | Restricted |

The persisted `DataClassification` enum is applied to selected entities that contain or govern these data: users, incidents and media, AI analysis and environmental context, Copilot conversations/messages/interactions/action proposals, audit logs, notifications, and knowledge documents. Defaults are conservative for incident and identity content; knowledge documents default to Internal and can be raised to Confidential. Classification is metadata and does not automatically grant access.

## Data minimization

Collect, expose, and retain only what an operational purpose requires. Anonymous submissions can still expose a person's home, face, vehicle, or private conversation. Avoid recording identity fields, exact locations, or media beyond the incident workflow's needs.
