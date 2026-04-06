# Design Document

## 1. System Overview

Demo Cart is a Kubernetes-hosted ecommerce demo platform with role-based access, order management, refund workflows, and observability.

Primary components:

1. UI service: React SPA served by Nginx
2. API service: .NET 8 Minimal API with Dapper
3. Data store: External SQL Server
4. Ingress: NGINX Ingress Controller
5. Observability: Prometheus, Grafana, Loki, Promtail

## 2. Architecture

### Runtime topology

1. Browser -> Ingress host
2. Ingress `/` -> ui-svc
3. Ingress `/api` -> api-svc
4. API -> SQL Server via DB_* environment configuration

### Auth model

1. API issues JWT on successful login.
2. JWT contains user id and role claim (`user` or `admin`).
3. UI sends bearer token for protected endpoints.

## 3. Functional Workflows

### Shopping workflow

1. Catalog load: `GET /api/catalog`
2. Add cart item: `POST /api/cart/items`
3. Cart fetch: `GET /api/cart`
4. Checkout: `POST /api/orders/checkout` (status `PendingPayment`)
5. Payment: `POST /api/payments/pay` (status `Paid`)

### Refund workflow

1. User submits refund reason:
  `POST /api/refunds/request`
2. User checks own requests:
  `GET /api/refunds/my`
3. Admin views queue:
  `GET /api/admin/refunds`
4. Admin decision (approve/reject with reason):
  `POST /api/admin/refunds/{id}/decision`
5. Approved request marks order as `Refunded`.

### Product tracking workflow

1. User requests tracking timeline:
  `GET /api/orders/{orderId}/tracking`
2. API returns tracking code, current status, and timeline steps.

### AI assistant workflow

1. UI chat submits prompt:
  `POST /api/chat/send`
2. API uses OpenAI key if configured, otherwise fallback assistant logic.

## 4. Configuration and Secrets

Configuration strategy:

1. ConfigMap for non-secret settings
2. Secret for DB credentials
3. Secret for JWT signing key
4. Secret for optional AI key

## 5. Reliability and Health

1. Liveness endpoint: `/health/live`
2. Readiness endpoint: `/health/ready` with DB check
3. Deployments run 2 replicas each for API and UI
4. Ingress sticky sessions enabled for better demo consistency

## 6. Fallback Behavior

When SQL connectivity is unavailable, selected API modules use in-memory fallback stores to keep demo flow operational for cart/order/payment and catalog continuity.

## 7. Security Considerations

1. JWT-based auth for protected APIs
2. Role checks on admin refund APIs
3. CORS restricted to known UI origins
4. Do not store production credentials in repository

## 8. Observability

1. Metrics from Prometheus
2. Dashboards in Grafana
3. Logs from Loki via Promtail
4. App-specific log dashboards for Demo Cart operations
