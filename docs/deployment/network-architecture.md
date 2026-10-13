# Staging Network Architecture

## Target topology

The initial staging preview targets a dedicated `rg-er-staging-eastus` resource group in East US. The planned path is:

```text
Browser -- HTTPS --> Web Container App
  |                       |
  | direct SignalR/API    | server-side API calls
  +-----------------------+----> API Container App
                                      |
                        VNet-integrated Container Apps environment
                                      |
                         SQL private endpoint + private DNS
```

The Web and API Container Apps are not included in the current Bicep preview because versioned images are not yet in ACR. If added later, Web ingress is external HTTPS and API ingress remains configurable and external for the current browser-to-API SignalR model. Worker deployment is deferred; when it exists, it must have no public ingress and must use durable shared job transport.

## VNet layout

The staging template currently proposes:

| Subnet | CIDR | Use |
|---|---|---|
| `aca-infrastructure` | `10.40.0.0/23` | Delegated to `Microsoft.App/environments` |
| `private-endpoints` | `10.40.2.0/24` | SQL private endpoint network interface; private endpoint network policies disabled |

The parent VNet is `10.40.0.0/16`. These are preview defaults only. Check overlap with peered, VPN, corporate, and on-premises networks before deployment. The Azure inventory alone cannot determine every connected network range.

## Environment and ingress

Container Apps environment networking is fixed at creation. The template uses `cae-er-staging-vnet` as the replacement VNet-integrated environment name and sets environment-level internal networking to false. This permits later apps to use external or internal ingress independently; it does not expose every app publicly.

The browser currently connects directly to the API SignalR hub at `/hubs/incidents`. Keep API HTTPS ingress externally reachable for the initial deployment until that flow, CORS, authorization, and reconnect behavior have been tested. Allow only the deployed Web origin. The current localhost-only CORS policy must be externalized before cloud staging. See [configuration reference](configuration-reference.md).

## Current status

The template and preview describe intended resources only. The subscription-scope `what-if` does not create the resource group, VNet, Container Apps environment, or any other Azure resource. Provider-level preflight reports that the subscription's one-Container-Apps-environment quota is already used by an existing environment. The template-level what-if can review proposed changes but skips provider quota and permission checks; request additional quota before provisioning. SQL private DNS resolution, private endpoint approval, workload routing, API/Web connectivity, and SignalR recovery remain unverified until a staging deployment is authorized and tested.
