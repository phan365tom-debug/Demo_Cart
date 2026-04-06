# Grafana Dashboard (7 Tiles Minimum)

Recommended dashboard: KubeCart-Operations

## Quick Setup (Prometheus + Grafana)

1. Install Helm if missing: https://helm.sh/docs/intro/install/
2. Run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/setup-grafana.ps1
```

3. Open Grafana:
	- http://localhost:3000
	- Username: admin
	- Password: admin123

The script installs kube-prometheus-stack into namespace monitoring and starts port-forward for Grafana.

Import option:

- Grafana -> Dashboards -> Import -> Upload docs/grafana-dashboard-7tiles.json
- Select your Prometheus datasource for DS_PROMETHEUS

## Tile 1: API Pod CPU Usage

- Query: sum(rate(container_cpu_usage_seconds_total{namespace="demo",pod=~"api-deploy.*",container!=""}[5m]))

## Tile 2: API Pod Memory Usage

- Query: sum(container_memory_working_set_bytes{namespace="demo",pod=~"api-deploy.*",container!=""})

## Tile 3: UI Pod CPU Usage

- Query: sum(rate(container_cpu_usage_seconds_total{namespace="demo",pod=~"ui-deploy.*",container!=""}[5m]))

## Tile 4: UI Pod Memory Usage

- Query: sum(container_memory_working_set_bytes{namespace="demo",pod=~"ui-deploy.*",container!=""})

## Tile 5: API Restart Count

- Query: sum(kube_pod_container_status_restarts_total{namespace="demo",pod=~"api-deploy.*"})

## Tile 6: Ready Pods by Deployment

- Query: sum(kube_deployment_status_replicas_ready{namespace="demo",deployment=~"api-deploy|ui-deploy"}) by (deployment)

## Tile 7: Service Request Error Rate (if ingress metrics enabled)

- Query: sum(rate(nginx_ingress_controller_requests{namespace="demo",status=~"5.."}[5m]))

## Notes

- Requires Prometheus scraping cAdvisor/kube-state-metrics/ingress-controller metrics.
- If metrics labels differ in your cluster, adjust namespace, pod, and deployment selectors.
