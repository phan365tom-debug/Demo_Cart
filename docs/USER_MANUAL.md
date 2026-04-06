# User Manual

## Access

Open one of the app URLs:

1. http://kubecart.local
2. https://democart.cloudflareaccess.com (if Cloudflare Access is configured)

## Login Modes

Use the login mode switch at the top:

1. User Login
2. Admin Login

Demo credentials:

1. User: user1 / User@123
2. Admin: admin / Admin@123

## User Flow

### Browse and Buy

1. Open Catalog tab.
2. Click Add to Cart for required products.
3. Open Cart tab and click Checkout.
4. Open Payment tab.
5. Fill customer details and shipping address.
6. Enter card last 4 digits.
7. Click Pay Now.

### Track Product

1. Open Orders tab.
2. Click Track Product for the selected order.
3. Review tracking code and timeline (placed, packed, shipped, out for delivery, delivered).

### Request Refund

1. Open Orders tab.
2. For a paid order, enter refund reason.
3. Click Request Refund.
4. Check status in Your Refund Requests section.

Possible statuses:

1. Pending
2. Approved
3. Rejected

If rejected, admin reason is displayed.

## Admin Flow

### Review Refund Queue

1. Login using Admin Login.
2. Open Admin Refunds tab.
3. Review each request with user and order details.

### Decide Refund

1. Click Approve Refund to accept.
2. Click Reject Refund to reject.
3. Enter rejection reason when rejecting.

## AI Sales Assistant

1. Click Chat with Sam.
2. Ask product or shopping questions.
3. Assistant responds using backend chat endpoint.

## Troubleshooting

1. If app does not open, verify ingress host mapping and minikube tunnel.
2. If login fails, verify DB seed data and API logs.
3. If refund request fails, ensure API deployment is updated and user order status is Paid.
