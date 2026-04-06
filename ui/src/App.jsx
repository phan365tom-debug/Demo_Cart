import { useEffect, useMemo, useRef, useState } from "react";

const API_BASE = import.meta.env.VITE_API_BASE || `${window.location.origin}/api`;

function categoryOf(name = "") {
  if (name.startsWith("Shoes")) return "shoes";
  if (name.startsWith("Clothes")) return "clothes";
  if (name.startsWith("Watch")) return "watch";
  if (name.startsWith("Electronics")) return "electronics";
  if (name.startsWith("System")) return "system";
  if (name.startsWith("Hardware")) return "hardware";
  return "general";
}

function categoryImageSet(category) {
  const sets = {
    shoes: [
      "https://images.pexels.com/photos/2529148/pexels-photo-2529148.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/1598505/pexels-photo-1598505.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/19090/pexels-photo.jpg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/1456738/pexels-photo-1456738.jpeg?auto=compress&cs=tinysrgb&w=1200"
    ],
    clothes: [
      "https://images.pexels.com/photos/996329/pexels-photo-996329.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/532220/pexels-photo-532220.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/934070/pexels-photo-934070.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/1036623/pexels-photo-1036623.jpeg?auto=compress&cs=tinysrgb&w=1200"
    ],
    watch: [
      "https://images.pexels.com/photos/277319/pexels-photo-277319.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/1697214/pexels-photo-1697214.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/280250/pexels-photo-280250.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/190819/pexels-photo-190819.jpeg?auto=compress&cs=tinysrgb&w=1200"
    ],
    electronics: [
      "https://images.pexels.com/photos/356056/pexels-photo-356056.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/699122/pexels-photo-699122.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/607812/pexels-photo-607812.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/1631439/pexels-photo-1631439.jpeg?auto=compress&cs=tinysrgb&w=1200"
    ],
    system: [
      "https://images.pexels.com/photos/18105/pexels-photo.jpg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/7974/pexels-photo.jpg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/1229861/pexels-photo-1229861.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/18104/pexels-photo.jpg?auto=compress&cs=tinysrgb&w=1200"
    ],
    hardware: [
      "https://images.pexels.com/photos/2582937/pexels-photo-2582937.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/1714208/pexels-photo-1714208.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/163100/circuit-circuit-board-resistor-computer-163100.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/3912981/pexels-photo-3912981.jpeg?auto=compress&cs=tinysrgb&w=1200"
    ],
    general: [
      "https://images.pexels.com/photos/5632402/pexels-photo-5632402.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/6214476/pexels-photo-6214476.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/3932872/pexels-photo-3932872.jpeg?auto=compress&cs=tinysrgb&w=1200",
      "https://images.pexels.com/photos/6956991/pexels-photo-6956991.jpeg?auto=compress&cs=tinysrgb&w=1200"
    ]
  };

  return sets[category] || sets.general;
}

function placeholderImage(name, angle) {
  const text = encodeURIComponent(`${name} ${angle}`);
  return `https://placehold.co/800x600/e5e7eb/1f2937?text=${text}`;
}

function productViewModel(product) {
  const category = categoryOf(product.name);
  const id = Number(product.id || 1);

  const images = categoryImageSet(category);
  const primaryImage = images[id % images.length];

  const gallery = [
    { label: "Front", url: primaryImage },
    { label: "Side", url: images[(id + 1) % images.length] },
    { label: "Top", url: images[(id + 2) % images.length] },
    { label: "Detail", url: images[(id + 3) % images.length] }
  ];

  const optionsByCategory = {
    shoes: { colors: ["Black", "White", "Blue"], sizes: ["40", "41", "42", "43", "44"] },
    clothes: { colors: ["Black", "Navy", "White"], sizes: ["S", "M", "L", "XL"] },
    watch: { colors: ["Black", "Silver", "Gold"], sizes: ["40mm", "44mm"] },
    electronics: { colors: ["Black", "Silver", "Gray"], sizes: [] },
    system: { colors: ["Black", "Silver", "White"], sizes: [] },
    hardware: { colors: ["Black", "Gray", "White"], sizes: [] },
    general: { colors: ["Default"], sizes: [] }
  };

  const specsByCategory = {
    shoes: ["Upper: Breathable mesh", "Sole: Anti-slip rubber", "Use: Daily + sports"],
    clothes: ["Fabric: Cotton blend", "Fit: Regular", "Care: Machine wash"],
    watch: ["Water resistance: 5 ATM", "Case: Stainless steel", "Warranty: 1 year"],
    electronics: ["Connectivity: Bluetooth/Wi-Fi", "Warranty: 1 year", "Box includes charger/cable"],
    system: ["CPU: Latest generation", "RAM: Expandable", "Storage: SSD based"],
    hardware: ["Interface: USB/PCIe", "Compatibility: Windows/Linux", "Warranty: 1 year"],
    general: ["Standard quality", "Seller warranty available"]
  };

  return {
    ...product,
    category,
    primaryImage,
    fallbackImage: placeholderImage(product.name, "Front"),
    gallery,
    colors: optionsByCategory[category].colors,
    sizes: optionsByCategory[category].sizes,
    specs: specsByCategory[category]
  };
}

function shippingStorageKey(user) {
  return `shippingByOrder:${user || "anonymous"}`;
}

function loadShippingMap(user) {
  try {
    const raw = localStorage.getItem(shippingStorageKey(user));
    if (!raw) return {};
    const parsed = JSON.parse(raw);
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
}

function saveShippingMap(user, map) {
  try {
    localStorage.setItem(shippingStorageKey(user), JSON.stringify(map || {}));
  } catch {
    // Ignore storage errors and keep runtime behavior.
  }
}

export default function App() {
  const [token, setToken] = useState(() => localStorage.getItem("token") || "");
  const [role, setRole] = useState(() => localStorage.getItem("role") || "user");
  const [username, setUsername] = useState(() => localStorage.getItem("username") || "");
  const [password, setPassword] = useState("");
  const [loginMode, setLoginMode] = useState("user");
  const [activeTab, setActiveTab] = useState("catalog");
  const [products, setProducts] = useState([]);
  const [cartItems, setCartItems] = useState([]);
  const [orders, setOrders] = useState([]);
  const [pendingOrderId, setPendingOrderId] = useState(0);
  const [shippingLine1, setShippingLine1] = useState("");
  const [shippingLine2, setShippingLine2] = useState("");
  const [shippingCity, setShippingCity] = useState("");
  const [shippingState, setShippingState] = useState("");
  const [shippingPostalCode, setShippingPostalCode] = useState("");
  const [shippingCountry, setShippingCountry] = useState("USA");
  const [shippingByOrderId, setShippingByOrderId] = useState(() => loadShippingMap(localStorage.getItem("username") || ""));
  const [refundReasonByOrder, setRefundReasonByOrder] = useState({});
  const [myRefunds, setMyRefunds] = useState([]);
  const [adminRefunds, setAdminRefunds] = useState([]);
  const [adminReasonByRefundId, setAdminReasonByRefundId] = useState({});
  const [trackingByOrderId, setTrackingByOrderId] = useState({});
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [emailAddress, setEmailAddress] = useState("");
  const [cardLast4, setCardLast4] = useState("");
  const [todos, setTodos] = useState([]);
  const [newTodo, setNewTodo] = useState("");
  const [chatInput, setChatInput] = useState("");
  const [chatOpen, setChatOpen] = useState(false);
  const [introPlayed, setIntroPlayed] = useState(false);
  const [selectedProduct, setSelectedProduct] = useState(null);
  const [chatMessages, setChatMessages] = useState([]);
  const typingTimerIds = useRef([]);
  const [message, setMessage] = useState("");
  const isLoggedIn = useMemo(() => Boolean(token), [token]);
  const cartTotal = useMemo(
    () => cartItems.reduce((sum, x) => sum + Number(x.lineTotal || 0), 0),
    [cartItems]
  );

  useEffect(() => {
    if (token) {
      loadTodos(token);
      loadProducts();
      loadCart(token);
      loadOrders(token);

      if (role === "admin") {
        loadAdminRefunds(token);
      } else {
        loadMyRefunds(token);
      }
    }
  }, [token, role]);

  useEffect(() => {
    if (!isLoggedIn) return;
    setShippingByOrderId(loadShippingMap(username));
  }, [isLoggedIn, username]);

  useEffect(() => {
    return () => {
      typingTimerIds.current.forEach((id) => clearInterval(id));
      typingTimerIds.current = [];
    };
  }, []);

  useEffect(() => {
    if (!chatOpen || introPlayed) return;

    setIntroPlayed(true);

    const introLines = [
      "Hi, I am Sam, your Demo Cart sales rep.",
      "Welcome to Demo Cart. Tell me what you need and I will help like a real store assistant."
    ];

    const delay = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

    const typeAiMessage = (text, speed = 22) => new Promise((resolve) => {
      let i = 0;
      setChatMessages((prev) => [...prev, { from: "ai", text: "" }]);

      const timerId = setInterval(() => {
        i += 1;
        setChatMessages((prev) => {
          const next = [...prev];
          const idx = next.length - 1;
          if (idx >= 0 && next[idx].from === "ai") {
            next[idx] = { ...next[idx], text: text.slice(0, i) };
          }
          return next;
        });

        if (i >= text.length) {
          clearInterval(timerId);
          resolve();
        }
      }, speed);

      typingTimerIds.current.push(timerId);
    });

    (async () => {
      for (const line of introLines) {
        await typeAiMessage(line);
        await delay(260);
      }
    })();
  }, [chatOpen, introPlayed]);

  async function login(e) {
    e.preventDefault();
    setMessage("Signing in...");

    const loginPath = loginMode === "admin" ? "/auth/admin/login" : "/auth/user/login";

    const res = await fetch(`${API_BASE}${loginPath}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password })
    });

    if (!res.ok) {
      const err = await readApiError(res, "Login failed. Check credentials.");
      setMessage(err);
      return;
    }

    const data = await res.json();
    setToken(data.token);
    setRole(data.role || loginMode);
    localStorage.setItem("token", data.token);
    localStorage.setItem("username", data.username);
    localStorage.setItem("role", data.role || loginMode);
    setMessage("Login success.");
  }

  async function apiGet(path, currentToken = token) {
    return fetch(`${API_BASE}${path}`, {
      headers: currentToken ? { Authorization: `Bearer ${currentToken}` } : {}
    });
  }

  async function apiPost(path, body, currentToken = token) {
    return fetch(`${API_BASE}${path}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...(currentToken ? { Authorization: `Bearer ${currentToken}` } : {})
      },
      body: JSON.stringify(body)
    });
  }

  async function readApiError(res, fallback) {
    try {
      const data = await res.json();
      if (data?.error && typeof data.error === "string") {
        return data.error;
      }
    } catch {
      // Ignore parse errors and return fallback message.
    }

    return fallback;
  }

  async function loadProducts() {
    const res = await apiGet("/catalog", "");
    if (!res.ok) {
      setMessage("Could not load catalog.");
      return;
    }
    setProducts(await res.json());
  }

  async function loadCart(currentToken = token) {
    const res = await apiGet("/cart", currentToken);
    if (!res.ok) {
      setMessage("Could not load cart.");
      return;
    }
    setCartItems(await res.json());
  }

  async function loadOrders(currentToken = token) {
    const res = await apiGet("/orders", currentToken);
    if (!res.ok) {
      setMessage("Could not load orders.");
      return;
    }

    const data = await res.json();
    setOrders(data);
    const pending = data.find((x) => x.status === "PendingPayment");
    setPendingOrderId(pending ? pending.id : 0);
  }

  async function loadMyRefunds(currentToken = token) {
    const res = await apiGet("/refunds/my", currentToken);
    if (!res.ok) {
      setMessage("Could not load refund requests.");
      return;
    }

    setMyRefunds(await res.json());
  }

  async function loadAdminRefunds(currentToken = token) {
    const res = await apiGet("/admin/refunds", currentToken);
    if (!res.ok) {
      setMessage("Could not load admin refund queue.");
      return;
    }

    setAdminRefunds(await res.json());
  }

  async function addToCart(productId) {
    const res = await apiPost("/cart/items", { productId, quantity: 1 });
    if (!res.ok) {
      setMessage("Failed to add item to cart.");
      return;
    }

    setMessage("Added to cart.");
    await loadCart();
  }

  async function removeFromCart(itemId) {
    const res = await fetch(`${API_BASE}/cart/items/${itemId}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${token}` }
    });

    if (!res.ok) {
      setMessage("Failed to remove item from cart.");
      return;
    }

    setMessage("Item removed.");
    await loadCart();
  }

  async function checkoutCart() {
    const res = await apiPost("/orders/checkout", {});
    if (!res.ok) {
      setMessage("Checkout failed. Ensure cart has items.");
      return;
    }

    const data = await res.json();
    setPendingOrderId(data.orderId);
    setMessage(`Order #${data.orderId} created. Complete payment.`);
    await Promise.all([loadCart(), loadOrders()]);
    setActiveTab("payment");
  }

  async function payNow(e) {
    e.preventDefault();
    if (!firstName.trim() || !lastName.trim() || !emailAddress.trim()) {
      setMessage("Please fill first name, last name, and email address.");
      return;
    }

    if (!/^\S+@\S+\.\S+$/.test(emailAddress.trim())) {
      setMessage("Please enter a valid email address.");
      return;
    }

    if (!shippingLine1.trim() || !shippingCity.trim() || !shippingState.trim() || !shippingPostalCode.trim() || !shippingCountry.trim()) {
      setMessage("Please complete shipping address in Payment section.");
      return;
    }

    const shippingSummary = [shippingLine1.trim(), shippingLine2.trim(), `${shippingCity.trim()}, ${shippingState.trim()} ${shippingPostalCode.trim()}`, shippingCountry.trim()]
      .filter(Boolean)
      .join(" | ");

    setShippingByOrderId((prev) => {
      const next = { ...prev, [Number(pendingOrderId)]: shippingSummary };
      saveShippingMap(username, next);
      return next;
    });

    if (cardLast4.length !== 4) {
      setMessage("Card last 4 digits must be exactly 4 numbers.");
      return;
    }

    const res = await apiPost("/payments/pay", {
      orderId: Number(pendingOrderId),
      cardHolderName: `${firstName.trim()} ${lastName.trim()}`,
      cardLast4
    });

    if (!res.ok) {
      const err = await readApiError(res, "Payment failed. Verify form values.");
      setMessage(err);
      return;
    }

    setFirstName("");
    setLastName("");
    setEmailAddress("");
    setCardLast4("");
    setMessage("Payment successful.");
    await loadOrders();
    await loadMyRefunds();
    setActiveTab("orders");
  }

  async function requestRefund(orderId) {
    const reason = (refundReasonByOrder[orderId] || "").trim();
    if (!reason) {
      setMessage("Please enter a refund reason first.");
      return;
    }

    const res = await apiPost("/refunds/request", { orderId, reason });
    if (!res.ok) {
      const err = await readApiError(res, "Refund request failed.");
      setMessage(err);
      return;
    }

    setRefundReasonByOrder((prev) => ({ ...prev, [orderId]: "" }));
    setMessage(`Refund request submitted for order #${orderId}.`);
    await Promise.all([loadMyRefunds(), loadOrders()]);
  }

  async function decideRefund(refundId, approve) {
    const reason = (adminReasonByRefundId[refundId] || "").trim();
    if (!approve && !reason) {
      setMessage("Rejection reason is required.");
      return;
    }

    const res = await apiPost(`/admin/refunds/${refundId}/decision`, { approve, reason });
    if (!res.ok) {
      const err = await readApiError(res, "Refund decision failed.");
      setMessage(err);
      return;
    }

    setAdminReasonByRefundId((prev) => ({ ...prev, [refundId]: "" }));
    setMessage(approve ? "Refund approved." : "Refund rejected.");
    await loadAdminRefunds();
  }

  async function trackOrder(orderId) {
    const res = await apiGet(`/orders/${orderId}/tracking`);
    if (!res.ok) {
      const err = await readApiError(res, "Tracking is not available for this order.");
      setMessage(err);
      return;
    }

    const data = await res.json();
    setTrackingByOrderId((prev) => ({ ...prev, [orderId]: data }));
  }

  async function loadTodos(currentToken = token) {
    const res = await fetch(`${API_BASE}/todos`, {
      headers: { Authorization: `Bearer ${currentToken}` }
    });

    if (!res.ok) {
      setMessage("Could not load todos.");
      return;
    }

    const data = await res.json();
    setTodos(data);
    setMessage("Todos loaded.");
  }

  async function addTodo(e) {
    e.preventDefault();
    if (!newTodo.trim()) return;

    const res = await fetch(`${API_BASE}/todos`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`
      },
      body: JSON.stringify({ title: newTodo })
    });

    if (res.ok) {
      setNewTodo("");
      await loadTodos();
    } else {
      setMessage("Failed to create todo.");
    }
  }

  async function toggleTodo(id) {
    const res = await fetch(`${API_BASE}/todos/${id}/toggle`, {
      method: "PUT",
      headers: { Authorization: `Bearer ${token}` }
    });

    if (res.ok) {
      await loadTodos();
    } else {
      setMessage("Failed to toggle todo.");
    }
  }

  function logout() {
    setToken("");
    setRole("user");
    setProducts([]);
    setCartItems([]);
    setOrders([]);
    setMyRefunds([]);
    setAdminRefunds([]);
    setTrackingByOrderId({});
    setTodos([]);
    setPassword("");
    setPendingOrderId(0);
    setShippingByOrderId({});
    localStorage.removeItem("token");
    localStorage.removeItem("username");
    localStorage.removeItem("role");
    setMessage("Logged out.");
  }

  async function sendChat(e) {
    e.preventDefault();
    if (!chatInput.trim()) return;

    const userMessage = chatInput.trim();
    setChatMessages((prev) => [...prev, { from: "user", text: userMessage }]);
    setChatInput("");

    const res = await apiPost("/chat/send", { message: userMessage }, "");
    if (!res.ok) {
      setChatMessages((prev) => [...prev, { from: "ai", text: "Assistant is temporarily unavailable." }]);
      return;
    }

    const data = await res.json();
    setChatMessages((prev) => [...prev, { from: "ai", text: data.reply }]);

    if (data.createdProduct) {
      setProducts((prev) => {
        if (prev.some((p) => p.id === data.createdProduct.id)) {
          return prev;
        }

        return [data.createdProduct, ...prev];
      });

      setActiveTab("catalog");
      setSelectedProduct(productViewModel(data.createdProduct));
      setMessage(`Sam found a matching option: ${data.createdProduct.name}`);
    }
  }

  return (
    <main className="page">
      <section className="card">
        <h1>Demo Cart</h1>
        <p className="subtitle">React + .NET 8 + SQL Server (external) + Minikube</p>

        {!isLoggedIn ? (
          <>
            <div className="login-mode-switch">
              <button
                type="button"
                className={loginMode === "user" ? "tab active" : "tab"}
                onClick={() => setLoginMode("user")}
              >
                User Login
              </button>
              <button
                type="button"
                className={loginMode === "admin" ? "tab active" : "tab"}
                onClick={() => setLoginMode("admin")}
              >
                Admin Login
              </button>
            </div>

            <p className="subtitle">
              {loginMode === "admin"
                ? "Admin portal credentials: admin / Admin@123"
                : "User portal credentials: user1 / User@123"}
            </p>

            <form onSubmit={login} className="form">
            <label>
              Username
              <input
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder={loginMode === "admin" ? "admin" : "user1"}
                required
              />
            </label>
            <label>
              Password
              <input
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={loginMode === "admin" ? "Admin@123" : "User@123"}
                required
              />
            </label>
            <button type="submit">{loginMode === "admin" ? "Login as Admin" : "Login as User"}</button>
            </form>
          </>
        ) : (
          <>
            <div className="toolbar">
              <p>Welcome, {username} ({role})</p>
              <button onClick={logout}>Logout</button>
            </div>

            <div className="tabs">
              <button onClick={() => setActiveTab("catalog")} className={activeTab === "catalog" ? "tab active" : "tab"}>Catalog</button>
              <button onClick={() => setActiveTab("cart")} className={activeTab === "cart" ? "tab active" : "tab"}>Cart</button>
              <button onClick={() => setActiveTab("payment")} className={activeTab === "payment" ? "tab active" : "tab"}>Payment</button>
              <button onClick={() => setActiveTab("orders")} className={activeTab === "orders" ? "tab active" : "tab"}>Orders</button>
              {role === "admin" && (
                <button onClick={() => setActiveTab("adminRefunds")} className={activeTab === "adminRefunds" ? "tab active" : "tab"}>Admin Refunds</button>
              )}
            </div>

            {activeTab === "catalog" && (
              <section className="grid">
                {products.map((product) => (
                  <article className="product" key={product.id}>
                    <img
                      src={productViewModel(product).primaryImage}
                      alt={product.name}
                      referrerPolicy="no-referrer"
                      onError={(e) => {
                        e.currentTarget.src = productViewModel(product).fallbackImage;
                      }}
                    />
                    <h3>{product.name}</h3>
                    <p>${Number(product.price).toFixed(2)}</p>
                    <div className="product-actions">
                      <button onClick={() => addToCart(product.id)}>Add to Cart</button>
                      <button onClick={() => setSelectedProduct(productViewModel(product))}>View</button>
                    </div>
                  </article>
                ))}
              </section>
            )}

            {activeTab === "cart" && (
              <section>
                <h2>Your Cart</h2>
                <ul className="list">
                  {cartItems.map((item) => (
                    <li key={item.id}>
                      <div className="cart-line">
                        <img
                          src={productViewModel({ id: item.productId, name: item.productName, price: item.price }).primaryImage}
                          alt={item.productName}
                          className="cart-thumb"
                          referrerPolicy="no-referrer"
                          onError={(e) => {
                            e.currentTarget.src = productViewModel({ id: item.productId, name: item.productName, price: item.price }).fallbackImage;
                          }}
                        />
                        <span>
                          {item.productName} x {item.quantity} (${Number(item.lineTotal).toFixed(2)})
                        </span>
                      </div>
                      <button onClick={() => removeFromCart(item.id)}>Remove</button>
                    </li>
                  ))}
                </ul>
                <p className="total">Total: ${cartTotal.toFixed(2)}</p>

                <button onClick={checkoutCart} disabled={!cartItems.length}>Checkout</button>
              </section>
            )}

            {activeTab === "payment" && (
              <section>
                <h2>Payment</h2>
                <p>Pending Order ID: {pendingOrderId || "None"}</p>
                {pendingOrderId > 0 && shippingByOrderId[pendingOrderId] && (
                  <p><strong>Shipping:</strong> {shippingByOrderId[pendingOrderId]}</p>
                )}

                <div className="shipping-card">
                  <h3>Customer Details</h3>
                  <div className="shipping-grid">
                    <input value={firstName} onChange={(e) => setFirstName(e.target.value)} placeholder="First name" />
                    <input value={lastName} onChange={(e) => setLastName(e.target.value)} placeholder="Last name" />
                    <input value={emailAddress} onChange={(e) => setEmailAddress(e.target.value)} placeholder="Email address" />
                  </div>
                </div>

                <div className="shipping-card">
                  <h3>Shipping Address</h3>
                  <div className="shipping-grid">
                    <input value={shippingLine1} onChange={(e) => setShippingLine1(e.target.value)} placeholder="Address line 1" />
                    <input value={shippingLine2} onChange={(e) => setShippingLine2(e.target.value)} placeholder="Address line 2 (optional)" />
                    <input value={shippingCity} onChange={(e) => setShippingCity(e.target.value)} placeholder="City" />
                    <input value={shippingState} onChange={(e) => setShippingState(e.target.value)} placeholder="State" />
                    <input value={shippingPostalCode} onChange={(e) => setShippingPostalCode(e.target.value)} placeholder="Postal code" />
                    <input value={shippingCountry} onChange={(e) => setShippingCountry(e.target.value)} placeholder="Country" />
                  </div>
                </div>

                <form className="form" onSubmit={payNow}>
                  <label>
                    Card Last 4 Digits
                    <input
                      value={cardLast4}
                      onChange={(e) => setCardLast4(e.target.value.replace(/\D/g, "").slice(0, 4))}
                      inputMode="numeric"
                      pattern="[0-9]{4}"
                      minLength={4}
                      maxLength={4}
                      placeholder="1234"
                      required
                    />
                  </label>
                  <button
                    type="submit"
                    disabled={
                      !pendingOrderId ||
                      !firstName.trim() ||
                      !lastName.trim() ||
                      !emailAddress.trim() ||
                      cardLast4.length !== 4
                    }
                  >
                    Pay Now
                  </button>
                </form>

                <div className="policy-card">
                  <h3>Refund Policy</h3>
                  <ul className="policy-list">
                    <li>Refund requests are accepted within 7 days of delivery.</li>
                    <li>Item must be unused and in original packaging.</li>
                    <li>Refund is processed to the original payment method within 5 to 10 business days.</li>
                    <li>For support, contact Demo Cart support with your order ID.</li>
                  </ul>
                </div>
              </section>
            )}

            {activeTab === "orders" && (
              <section>
                <h2>Orders</h2>
                <ul className="list">
                  {orders.map((order) => (
                    <li key={order.id} className="order-item">
                      <span className="order-line">
                        Order #{order.id} - ${Number(order.totalAmount).toFixed(2)} - {order.status}
                        <br />Shipping: {shippingByOrderId[order.id] || "Not captured for this order"}
                      </span>

                      {role !== "admin" && (
                        <div className="order-actions">
                          <button onClick={() => trackOrder(order.id)}>Track Product</button>

                          {order.status === "Paid" && (
                            <>
                              <input
                                value={refundReasonByOrder[order.id] || ""}
                                onChange={(e) => setRefundReasonByOrder((prev) => ({ ...prev, [order.id]: e.target.value }))}
                                placeholder="Reason for refund"
                              />
                              <button onClick={() => requestRefund(order.id)}>Request Refund</button>
                            </>
                          )}
                        </div>
                      )}

                      {trackingByOrderId[order.id] && (
                        <div className="tracking-card">
                          <p><strong>Tracking ID:</strong> {trackingByOrderId[order.id].trackingCode}</p>
                          <p><strong>Current Status:</strong> {trackingByOrderId[order.id].currentStatus}</p>
                          <ul className="policy-list">
                            {(trackingByOrderId[order.id].timeline || []).map((step) => (
                              <li key={step}>{step}</li>
                            ))}
                          </ul>
                        </div>
                      )}
                    </li>
                  ))}
                </ul>

                {role !== "admin" && (
                  <div className="policy-card">
                    <h3>Your Refund Requests</h3>
                    <ul className="list">
                      {myRefunds.map((item) => (
                        <li key={item.id} className="order-item">
                          <span className="order-line">
                            Request #{item.id} - Order #{item.orderId} - {item.status}
                            <br />Reason: {item.requestReason}
                            {item.adminReason && <><br />Admin Note: {item.adminReason}</>}
                          </span>
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                <div className="policy-card">
                  <h3>Refund Policy</h3>
                  <ul className="policy-list">
                    <li>Paid orders can be requested for refund within 7 days.</li>
                    <li>Refund approval depends on product condition and verification.</li>
                    <li>Once approved, refund reaches your payment source within 5 to 10 business days.</li>
                  </ul>
                </div>
              </section>
            )}

            {activeTab === "adminRefunds" && role === "admin" && (
              <section>
                <h2>Admin Refund Queue</h2>
                <ul className="list">
                  {adminRefunds.map((item) => (
                    <li key={item.id} className="order-item">
                      <span className="order-line">
                        Request #{item.id} - Order #{item.orderId} - User: {item.username}
                        <br />Status: {item.status}
                        <br />Request Reason: {item.requestReason}
                        {item.adminReason && <><br />Decision Reason: {item.adminReason}</>}
                      </span>

                      {item.status === "Pending" && (
                        <div className="order-actions">
                          <input
                            value={adminReasonByRefundId[item.id] || ""}
                            onChange={(e) => setAdminReasonByRefundId((prev) => ({ ...prev, [item.id]: e.target.value }))}
                            placeholder="Reason (required for reject)"
                          />
                          <button onClick={() => decideRefund(item.id, true)}>Approve Refund</button>
                          <button className="danger-btn" onClick={() => decideRefund(item.id, false)}>Reject Refund</button>
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              </section>
            )}

            {activeTab === "todos" && (
              <>
                <form onSubmit={addTodo} className="form-inline">
                  <input
                    value={newTodo}
                    onChange={(e) => setNewTodo(e.target.value)}
                    placeholder="Add a todo"
                    required
                  />
                  <button type="submit">Add</button>
                </form>

                <ul className="list">
                  {todos.map((todo) => (
                    <li key={todo.id}>
                      <span className={todo.isDone ? "done" : ""}>{todo.title}</span>
                      <button onClick={() => toggleTodo(todo.id)}>Toggle</button>
                    </li>
                  ))}
                </ul>
              </>
            )}

          </>
        )}

        {selectedProduct && (
          <div className="modal-backdrop" onClick={() => setSelectedProduct(null)}>
            <section className="modal" onClick={(e) => e.stopPropagation()}>
              <div className="modal-head">
                <h2>{selectedProduct.name}</h2>
                <button onClick={() => setSelectedProduct(null)}>Close</button>
              </div>

              <img
                className="modal-main-image"
                src={selectedProduct.primaryImage}
                alt={selectedProduct.name}
                referrerPolicy="no-referrer"
                onError={(e) => {
                  e.currentTarget.src = selectedProduct.fallbackImage;
                }}
              />

              <div className="thumb-row">
                {selectedProduct.gallery.map((g) => (
                  <div className="thumb-item" key={g.label}>
                    <img
                      src={g.url}
                      alt={`${selectedProduct.name} ${g.label}`}
                      referrerPolicy="no-referrer"
                      onError={(e) => {
                        e.currentTarget.src = placeholderImage(selectedProduct.name, g.label);
                      }}
                    />
                    <small>{g.label}</small>
                  </div>
                ))}
              </div>

              {selectedProduct.colors.length > 0 && (
                <p><strong>Colors:</strong> {selectedProduct.colors.join(", ")}</p>
              )}

              {selectedProduct.sizes.length > 0 && (
                <p><strong>Sizes:</strong> {selectedProduct.sizes.join(", ")}</p>
              )}

              <p><strong>Specifications:</strong></p>
              <ul className="spec-list">
                {selectedProduct.specs.map((s) => (
                  <li key={s}>{s}</li>
                ))}
              </ul>
            </section>
          </div>
        )}

        <p className="message">{message}</p>
      </section>

      <section className="sales-chat-shell" aria-label="Sales assistant">
        <button className="sales-chat-toggle" onClick={() => setChatOpen((v) => !v)}>
          <span className="sales-avatar" aria-hidden="true">🧑</span>
          <span>{chatOpen ? "Hide Sam" : "Chat with Sam"}</span>
        </button>

        {chatOpen && (
          <div className="sales-chat-panel">
            <header className="sales-chat-head">
              <h3>Sam | Sales Assistant</h3>
              <small>Online now</small>
            </header>
            <div className="chat-box">
              {chatMessages.map((m, i) => (
                <p key={i} className={m.from === "ai" ? "ai" : "user"}>
                  <strong>{m.from === "ai" ? "Sam" : "You"}:</strong> {m.text}
                </p>
              ))}
            </div>
            <form onSubmit={sendChat} className="form-inline sales-chat-form">
              <input value={chatInput} onChange={(e) => setChatInput(e.target.value)} placeholder="Ask Sam anything" />
              <button type="submit">Send</button>
            </form>
          </div>
        )}
      </section>
    </main>
  );
}
