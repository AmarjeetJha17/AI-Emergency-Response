# Staging Verification

## Preview

The preview target is `rg-er-staging-eastus` in East US. From the repository root, run:

```powershell
az bicep build --file infra/main.bicep
az bicep build --file infra/staging-preview.bicep
./scripts/preview-staging-network.ps1 -ValidationLevel Template
```

The script reads the active subscription and signed-in Entra user, then runs a subscription-level ARM `what-if`. It does not deploy resources. `Template` validation skips provider preflight and permission checks, so this preview is for reviewing planned resource changes only. Provider-level validation currently fails because the subscription has reached its one-Container-Apps-environment quota and already contains an environment. Review all proposed changes, including creation of the dedicated resource group if it is absent, and request quota before deployment. Confirm the subscription context before running it.

The initial values are East US, VNet `10.40.0.0/16`, ACA subnet `10.40.0.0/23`, private-endpoint subnet `10.40.2.0/24`, and SQL SKU `GP_S_Gen5_1`. Check network overlap, regional service availability, SQL SKU availability, data residency, global naming, and cost before any real deployment. Template-level `what-if` does not validate provider policies, quotas, permissions, name availability, runtime identities, image existence, or private network behavior.

## Deployment verification status

No staging resources or role assignments have been provisioned by this implementation. The template-level preview reports 26 planned resource changes, including the registry-scoped `AcrPull` assignment. The following checks are pending until a deployment is separately authorized:

- Confirm Container Apps environment is attached to the intended VNet/subnet.
- Confirm SQL Entra-only authentication, disabled public network access, approved private endpoint, and private DNS resolution from the VNet.
- Create contained API/Worker database users and verify least-privilege operations using the workload identities.
- Push immutable Web/API images to ACR and verify identity-based pulls.
- Configure Web external HTTPS and API ingress for the browser's direct SignalR route; verify CORS and authorization.
- Verify application health, SQL access, SignalR reconnect/snapshot refresh, telemetry, and worker/queue recovery.

The Worker remains deferred until its host exists and all relevant job paths use a durable cross-process transport. This preview therefore does not satisfy the runtime connectivity portion of Phase 17.3 acceptance.
