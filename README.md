# KubeCart Capstone (Minikube + External SQL Server)

This repository contains a production-style demo app deployed to Minikube:

- Frontend: React (Vite)
- Backend: .NET 8 Minimal API + Dapper
- Database: SQL Server running outside Kubernetes (host machine)
- Kubernetes: Deployments, Services, Ingress, ConfigMap, Secrets, probes, scaling

Feature modules included:

- Auth login
- Catalog browsing
- Add to cart and cart view
- Checkout to create order
- Payment page to complete pending orders
- Orders history
- Todo list
- AI live chat support endpoint and UI panel

## Repo Structure

- Api
- ui
- db
- k8s
- docs

## Prerequisites

- Docker Desktop or Docker Engine
- Minikube
- kubectl
- .NET 8 SDK
- Node.js 20+
- SQL Server on host machine (local install or Docker on host)

## 1. Initialize External SQL Server (Host Machine)

Run:

1. db/01_schema.sql
2. db/02_seed.sql

Note: db/02_seed.sql now seeds a large Amazon-style catalog across multiple categories (5 items per category) and can be re-run safely.

Default seeded user:

- username: admin
- password: Admin@123

## 2. Configure Host DB Access for Minikube

This project uses host.minikube.internal for SQL Server host connectivity.

If your environment needs host IP instead, update DB_HOST in k8s/01-configmap-api.yaml.

Why this is required:

- localhost inside a pod points to the pod itself, not your laptop host.

## 3. Build Images Inside Minikube Docker

Use PowerShell:

```powershell
minikube -p minikube docker-env | Invoke-Expression
docker build -t kubecart-api:latest ./Api
docker build -t kubecart-ui:latest ./ui
```

## 4. Enable Ingress + Apply Manifests

```powershell
minikube addons enable ingress
kubectl apply -f k8s/00-namespace.yaml
kubectl apply -f k8s/01-configmap-api.yaml
kubectl apply -f k8s/02-secret-db.yaml
kubectl apply -f k8s/03-secret-jwt.yaml
kubectl apply -f k8s/09-secret-ai.yaml
kubectl apply -f k8s/04-deployment-api.yaml
kubectl apply -f k8s/05-service-api.yaml
kubectl apply -f k8s/06-deployment-ui.yaml
kubectl apply -f k8s/07-service-ui.yaml
kubectl apply -f k8s/08-ingress.yaml
```

Or use one command:

```powershell
./scripts/deploy-minikube.ps1
```

## 5. Map Hostname and Open App

Add to hosts file:

- 127.0.0.1 kubecart.local

Then run:

```powershell
minikube tunnel
```

Open:

- http://kubecart.local

## 6. Validate Required Outputs

```powershell
kubectl get pods -n demo
kubectl get svc -n demo
kubectl describe ingress -n demo kubecart-ingress
kubectl logs -n demo deploy/api-deploy
```

## API Endpoints

- GET /health/live
- GET /health/ready
- POST /api/auth/login
- GET /api/todos
- POST /api/todos
- PUT /api/todos/{id}/toggle
- GET /api/catalog
- GET /api/cart
- POST /api/cart/items
- DELETE /api/cart/items/{id}
- POST /api/orders/checkout
- GET /api/orders
- POST /api/payments/pay
- POST /api/chat/send

## Why Readiness and Liveness are Different

- Readiness determines when a pod can receive traffic. Here it checks DB connectivity through /health/ready.
- Liveness determines if the process is alive. Here /health/live only validates app process health.

This prevents sending traffic to a pod that is up but cannot reach SQL Server.

## Scaling Requirement

Both ui and api deployments are set to 2 replicas:

- k8s/04-deployment-api.yaml
- k8s/06-deployment-ui.yaml

## Grafana Requirement

See docs/GRAFANA.md for a 7-tile dashboard plan and Prometheus queries.
Importable JSON template is included in docs/grafana-dashboard-7tiles.json.
Use scripts/setup-grafana.ps1 for one-command Grafana bootstrap.

## Screenshots and Demo Evidence

Capture and store evidence in docs/screenshots based on docs/SCREENSHOT_CHECKLIST.md.

## Security Notes

- Do not commit real production credentials.
- Replace values in k8s/02-secret-db.yaml and k8s/03-secret-jwt.yaml in your environment.
