# Demo Cart

Demo Cart is a cloud-native ecommerce demo application for capstone submission.

## Start Here (Non-Technical)

If you are a reviewer or stakeholder without technical background, read:

1. docs/README_NON_TECHNICAL.md
2. README_NON_TECHNICAL.md

Stack:

- UI: React + Vite + Nginx
- API: .NET 8 Minimal API + Dapper
- Data: External SQL Server
- Platform: Kubernetes (Minikube)
- Observability: Prometheus + Grafana + Loki + Promtail

Core capabilities:

- Role-based login (admin and user)
- Catalog, cart, checkout, payment, orders
- Shipping details capture in payment flow
- Refund request workflow (user) and decision workflow (admin)
- Product tracking timeline for users
- AI sales assistant chat panel

## Clean Repo Structure

```text
Shuaib_cart/
	Api/                     .NET 8 API project
	ui/                      React app (Vite) and Nginx config
	db/                      SQL schema and seed scripts
	k8s/                     Kubernetes manifests
	scripts/                 Deployment and setup scripts
	docs/                    User manual, design, debugging, grafana docs
```

## For DevOps

### Prerequisites

- Docker Desktop or Docker Engine
- Minikube
- kubectl
- SQL Server running on host machine or reachable network host

### Provision Database

Run these scripts in SQL Server:

1. db/01_schema.sql
2. db/02_seed.sql

### Deploy to Minikube

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

Or run:

```powershell
./scripts/deploy-minikube.ps1
```

### Verification

```powershell
kubectl get pods -n demo
kubectl get svc -n demo
kubectl get ingress -n demo
kubectl logs -n demo deploy/api-deploy --tail=100
```

### Access

- Local host: http://kubecart.local
- Cloudflare host (if configured): https://democart.cloudflareaccess.com

## For Developers

### Prerequisites

- .NET SDK 8+
- Node.js 20+
- npm

### API local run

```powershell
cd Api
dotnet restore
dotnet run
```

### UI local run

```powershell
cd ui
npm install
npm run dev
```

By default, UI calls API via same-origin `/api` in production build.

## API Surface (high level)

- Health: /health/live, /health/ready
- Auth: /api/auth/user/login, /api/auth/admin/login
- Catalog/Cart/Orders/Payments
- Refund workflow:
	- User: /api/refunds/request, /api/refunds/my
	- Admin: /api/admin/refunds, /api/admin/refunds/{id}/decision
- Tracking: /api/orders/{orderId}/tracking
- Chat: /api/chat/send

## Documentation Map

- User Manual: docs/USER_MANUAL.md
- Design Document: docs/DESIGN.md
- Non-Technical Guide: docs/README_NON_TECHNICAL.md
- Grafana Guide: docs/GRAFANA.md
- Debug Guide: docs/DEBUGGING.md
- Screenshot Checklist: docs/SCREENSHOT_CHECKLIST.md

## Security Notes

- Replace demo credentials and secret values before any real deployment.
- Do not commit production keys or real DB credentials.
