# Design Document

## Architecture

- UI (React + Nginx) in Kubernetes
- API (.NET 8 + Dapper) in Kubernetes
- SQL Server external to Kubernetes on host machine

## Request Flow

1. Browser requests kubecart.local
2. Ingress routes:
   - / to ui-svc
   - /api to api-svc
3. API uses DB_* env vars to connect to external SQL Server
4. API authenticates user and returns JWT
5. UI stores token and calls protected todos endpoints

Commerce flow:

1. UI reads product catalog from /api/catalog
2. User adds items via /api/cart/items
3. UI reads cart via /api/cart
4. Checkout creates order in PendingPayment state via /api/orders/checkout
5. Payment page confirms order via /api/payments/pay and marks order as Paid

AI support flow:

1. UI sends user message to /api/chat/send
2. API returns assistant guidance for usage and troubleshooting

## Configuration Strategy

- Non-sensitive config in ConfigMap
- Sensitive values in separate Secrets:
  - db-secret (DB_USER, DB_PASSWORD)
  - jwt-secret (JWT_SIGNING_KEY)

## Health Strategy

- Liveness: /health/live (process level)
- Readiness: /health/ready (includes DB query check)

## Scaling

- API and UI each run at 2 replicas
- Services route traffic across ready pods

## Operational Notes

- External dependency failures surface as readiness failures
- This prevents broken API pods from receiving production traffic
- Order and payment are transactional in SQL Server to avoid partial checkout writes
