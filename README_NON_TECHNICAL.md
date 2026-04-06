# Demo Cart - Non-Technical Guide

This guide is written for non-technical readers.

If you are a manager, client, reviewer, or stakeholder, this is the right file for you.

## What Is Demo Cart?

Demo Cart is a sample online shopping application.

It demonstrates a real business flow from product selection to payment, delivery tracking, and refund handling.

## What You Can Do in Demo Cart

1. Login as user or admin
2. Browse products and add to cart
3. Checkout and make payment
4. Track product status after order is placed
5. Request refund with reason
6. Approve or reject refund as admin

## Roles in the System

1. User
	- Shops for products
	- Pays for orders
	- Tracks delivery progress
	- Requests refunds

2. Admin
	- Reviews refund requests
	- Approves or rejects refund
	- Adds reason when rejecting

## Demo Login Details

User login:

1. Username: user1
2. Password: User@123

Admin login:

1. Username: admin
2. Password: Admin@123

## Simple User Journey

1. Login as user.
2. Open Catalog and add items.
3. Open Cart and click Checkout.
4. Open Payment and fill details.
5. Click Pay Now.
6. Open Orders.
7. Click Track Product to view progress.
8. If needed, submit refund request with reason.

## Simple Admin Journey

1. Login as admin.
2. Open Admin Refunds tab.
3. Read user refund reason.
4. Approve refund or reject refund.
5. If rejecting, add rejection reason.

## What Tracking Means

Tracking can show steps like:

1. Order placed
2. Payment confirmed
3. Packed
4. Shipped
5. Out for delivery
6. Delivered

## What Refund Status Means

1. Pending: waiting for admin decision
2. Approved: refund accepted
3. Rejected: refund denied with reason

## Where to Open Demo Cart

Use one of these URLs based on your setup:

1. http://kubecart.local
2. https://democart.cloudflareaccess.com

## Common Issues (Easy Explanation)

1. App page not opening
	- Server may not be running. Contact project owner.

2. Login failed
	- Check username and password exactly.

3. Refund request failed
	- Refund is allowed only for paid orders.

4. Cloudflare shows NoAuth
	- Your email is not yet allowed in Cloudflare Access policy.

## Why This Project Matters

Demo Cart shows a full real-world ecommerce process in one application:

1. Shopping experience
2. Payment process
3. Delivery tracking
4. Refund governance
5. Admin decision workflow

If you need technical setup and deployment steps, use README.md.
