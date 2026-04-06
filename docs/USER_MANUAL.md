# User Manual

## Login

1. Open http://kubecart.local
2. Enter:
   - Username: admin
   - Password: Admin@123
3. Click Login

## Todo Operations

- View existing todos after login
- Add todo using Add field
- Toggle done/undone using Toggle button
- Logout using Logout button

## Catalog and Cart

- Open Catalog tab to browse products
- Click Add to Cart on any product
- Open Cart tab to view selected items
- Click Remove to delete an item

## Checkout and Payment

- In Cart tab, click Checkout to create pending order
- Open Payment tab and verify pending order ID
- Enter card holder name and card last 4 digits
- Click Pay Now to mark order as paid
- Open Orders tab to see final status

## AI Live Chat Support

- Open AI Chat tab
- Ask questions about login, cart, payment, or troubleshooting
- Assistant replies from backend endpoint /api/chat/send
- If OPENAI_API_KEY is configured in k8s/09-secret-ai.yaml, chat uses OpenAI.
- If no key is configured, chat uses built-in fallback support responses.

## Troubleshooting for Users

- If page does not load, check ingress and host mapping.
- If login fails, verify API pod logs and DB seed user.
