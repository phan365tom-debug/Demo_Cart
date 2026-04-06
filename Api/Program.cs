using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddHttpClient();

var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "kubecart-api";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "kubecart-ui";
var jwtSigningKey = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY") ?? "change-this-signing-key-in-prod";
var allowDegradedReadiness = bool.TryParse(Environment.GetEnvironmentVariable("ALLOW_DEGRADED_READINESS"), out var degraded) ? degraded : true;
var demoLoginEnabled = bool.TryParse(Environment.GetEnvironmentVariable("DEMO_LOGIN_ENABLED"), out var demoLogin) ? demoLogin : true;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ui", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://kubecart.local",
                "https://democart.cloudflareaccess.com")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("ui");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { service = "kubecart-api", status = "ok" }));
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));

app.MapGet("/health/ready", async () =>
{
    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        await conn.OpenAsync();
        var one = await conn.ExecuteScalarAsync<int>("SELECT 1");
        return one == 1
            ? Results.Ok(new { status = "ready", db = "ok" })
            : Results.Problem("DB check did not return expected result.");
    }
    catch (Exception ex)
    {
        if (allowDegradedReadiness)
        {
            return Results.Ok(new { status = "ready", db = "degraded", reason = ex.Message });
        }

        return Results.Problem($"Readiness failed: {ex.Message}");
    }
});

async Task<IResult> LoginByRole(LoginRequest req, string requiredRole)
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    UserRecord? user;

    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        user = await conn.QuerySingleOrDefaultAsync<UserRecord>(
            "SELECT Id, Username, PasswordPlain FROM Users WHERE Username = @username",
            new { username = req.Username });
    }
    catch
    {
        user = null;
    }

    if (user is null && demoLoginEnabled)
    {
        if (req.Username == "admin" && req.Password == "Admin@123")
        {
            user = new UserRecord(1, "admin", "Admin@123");
        }
        else if (req.Username == "user1" && req.Password == "User@123")
        {
            user = new UserRecord(2, "user1", "User@123");
        }
    }

    if (user is null || user.PasswordPlain != req.Password)
    {
        return Results.Unauthorized();
    }

    var role = user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase) ? "admin" : "user";
    if (!role.Equals(requiredRole, StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = $"Use the {role} login page for this account." });
    }

    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, role)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: creds);

    var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new LoginResponse(tokenValue, user.Username, role));
}

async Task<OrderLookupResult?> FindOrderForUserAsync(int orderId, int userId)
{
    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        var row = await conn.QuerySingleOrDefaultAsync<OrderLookupResult>(
            @"SELECT Id, UserId, Status, CreatedAt
              FROM Orders
              WHERE Id = @orderId",
            new { orderId });

        if (row is not null)
        {
            return row;
        }
    }
    catch
    {
        // Fallback lookup is attempted below.
    }

    var fallback = FallbackOrderStore.TryGetOrder(userId, orderId);
    if (fallback is null)
    {
        return null;
    }

    return new OrderLookupResult(fallback.Id, userId, fallback.Status, fallback.CreatedAt);
}

async Task<bool> TryMarkOrderRefundedAsync(int orderId, int userId)
{
    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        var changed = await conn.ExecuteAsync(
            "UPDATE Orders SET Status = 'Refunded' WHERE Id = @orderId AND UserId = @userId",
            new { orderId, userId });

        if (changed > 0)
        {
            return true;
        }
    }
    catch
    {
        // Fallback status update is attempted below.
    }

    return FallbackOrderStore.UpdateStatus(userId, orderId, "Refunded");
}

app.MapPost("/api/auth/login", (LoginRequest req) => LoginByRole(req, "user"));
app.MapPost("/api/auth/user/login", (LoginRequest req) => LoginByRole(req, "user"));
app.MapPost("/api/auth/admin/login", (LoginRequest req) => LoginByRole(req, "admin"));

app.MapGet("/api/todos", async (HttpContext ctx) =>
{
    var userIdStr = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (!int.TryParse(userIdStr, out var userId))
    {
        return Results.Unauthorized();
    }

    await using var conn = new SqlConnection(BuildConnectionString());
    var rows = await conn.QueryAsync<TodoItem>(
        "SELECT Id, UserId, Title, IsDone FROM Todos WHERE UserId = @userId ORDER BY Id DESC",
        new { userId });

    return Results.Ok(rows);
}).RequireAuthorization();

app.MapPost("/api/todos", async (HttpContext ctx, CreateTodoRequest req) =>
{
    var userIdStr = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (!int.TryParse(userIdStr, out var userId))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(req.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }

    await using var conn = new SqlConnection(BuildConnectionString());
    var newId = await conn.ExecuteScalarAsync<int>(
        @"INSERT INTO Todos(UserId, Title, IsDone)
          OUTPUT INSERTED.Id
          VALUES (@userId, @title, 0)",
        new { userId, title = req.Title.Trim() });

    return Results.Created($"/api/todos/{newId}", new { id = newId, title = req.Title.Trim(), isDone = false });
}).RequireAuthorization();

app.MapPut("/api/todos/{id:int}/toggle", async (HttpContext ctx, int id) =>
{
    var userIdStr = ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
    if (!int.TryParse(userIdStr, out var userId))
    {
        return Results.Unauthorized();
    }

    await using var conn = new SqlConnection(BuildConnectionString());
    var changed = await conn.ExecuteAsync(
        @"UPDATE Todos
          SET IsDone = CASE WHEN IsDone = 1 THEN 0 ELSE 1 END
          WHERE Id = @id AND UserId = @userId",
        new { id, userId });

    return changed == 0 ? Results.NotFound() : Results.NoContent();
}).RequireAuthorization();

app.MapGet("/api/catalog", async () =>
{
    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        var products = await conn.QueryAsync<ProductItem>(
            @"SELECT Id, Name, Price, ImageUrl
              FROM Products
              WHERE IsActive = 1
              ORDER BY Id");

        return Results.Ok(products);
    }
    catch
    {
        // Keep catalog visible even when external SQL is down.
        return Results.Ok(FallbackCatalogStore.GetAll());
    }
});

app.MapGet("/api/cart", async (HttpContext ctx) =>
{
    var userId = TryGetUserId(ctx) ?? 1;

    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        var items = await conn.QueryAsync<CartItemResponse>(
            @"SELECT c.Id, c.ProductId, p.Name AS ProductName, p.Price, c.Quantity,
                     p.ImageUrl,
                     CAST((p.Price * c.Quantity) AS DECIMAL(10,2)) AS LineTotal
              FROM CartItems c
              JOIN Products p ON p.Id = c.ProductId
              WHERE c.UserId = @userId
              ORDER BY c.Id DESC",
            new { userId });

        return Results.Ok(items);
    }
    catch
    {
        return Results.Ok(FallbackCartStore.GetItems(userId));
    }
    });

app.MapPost("/api/cart/items", async (HttpContext ctx, AddCartItemRequest req) =>
{
    var userId = TryGetUserId(ctx) ?? 1;

    if (req.ProductId <= 0 || req.Quantity <= 0)
    {
        return Results.BadRequest(new { error = "ProductId and Quantity must be positive." });
    }

    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        var exists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Products WHERE Id = @id AND IsActive = 1",
            new { id = req.ProductId });

        if (exists == 0)
        {
            return Results.NotFound(new { error = "Product not found." });
        }

        await conn.ExecuteAsync(
            @"MERGE CartItems AS target
              USING (SELECT @userId AS UserId, @productId AS ProductId) AS source
              ON target.UserId = source.UserId AND target.ProductId = source.ProductId
              WHEN MATCHED THEN
                UPDATE SET Quantity = target.Quantity + @quantity, UpdatedAt = SYSUTCDATETIME()
              WHEN NOT MATCHED THEN
                INSERT(UserId, ProductId, Quantity, CreatedAt, UpdatedAt)
                VALUES(@userId, @productId, @quantity, SYSUTCDATETIME(), SYSUTCDATETIME());",
            new { userId, productId = req.ProductId, quantity = req.Quantity });

        return Results.Ok(new { message = "Item added to cart." });
    }
    catch
    {
        var product = FallbackCatalogStore.GetAll().FirstOrDefault(p => p.Id == req.ProductId);
        if (product is null)
        {
            return Results.NotFound(new { error = "Product not found." });
        }

        FallbackCartStore.Add(userId, product, req.Quantity);
        return Results.Ok(new { message = "Item added to cart (fallback)." });
    }
    });

app.MapDelete("/api/cart/items/{id:int}", async (HttpContext ctx, int id) =>
{
    var userId = TryGetUserId(ctx) ?? 1;

    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        var changed = await conn.ExecuteAsync(
            "DELETE FROM CartItems WHERE Id = @id AND UserId = @userId",
            new { id, userId });

        return changed == 0 ? Results.NotFound() : Results.NoContent();
    }
    catch
    {
        var changed = FallbackCartStore.Remove(userId, id);
        return changed ? Results.NoContent() : Results.NotFound();
    }
    });

app.MapPost("/api/orders/checkout", async (HttpContext ctx) =>
{
    var userId = TryGetUserId(ctx);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var cartItems = (await conn.QueryAsync<CartCheckoutRow>(
            @"SELECT c.ProductId, c.Quantity, p.Price
              FROM CartItems c
              JOIN Products p ON p.Id = c.ProductId
              WHERE c.UserId = @userId",
            new { userId }, tx)).ToList();

        if (cartItems.Count == 0)
        {
            await tx.RollbackAsync();
            return Results.BadRequest(new { error = "Cart is empty." });
        }

        var total = cartItems.Sum(x => x.Price * x.Quantity);

        var orderId = await conn.ExecuteScalarAsync<int>(
            @"INSERT INTO Orders(UserId, TotalAmount, Status, CreatedAt)
              OUTPUT INSERTED.Id
              VALUES(@userId, @total, 'PendingPayment', SYSUTCDATETIME())",
            new { userId, total }, tx);

        foreach (var row in cartItems)
        {
            await conn.ExecuteAsync(
                @"INSERT INTO OrderItems(OrderId, ProductId, Quantity, UnitPrice)
                  VALUES(@orderId, @productId, @quantity, @unitPrice)",
                new
                {
                    orderId,
                    productId = row.ProductId,
                    quantity = row.Quantity,
                    unitPrice = row.Price
                },
                tx);
        }

        await conn.ExecuteAsync("DELETE FROM CartItems WHERE UserId = @userId", new { userId }, tx);
        await tx.CommitAsync();

        return Results.Ok(new { orderId, total, status = "PendingPayment" });
    }
    catch
    {
        var fallbackItems = FallbackCartStore.GetItems(userId.Value).ToList();
        if (fallbackItems.Count == 0)
        {
            return Results.BadRequest(new { error = "Cart is empty." });
        }

        var created = FallbackOrderStore.Create(userId.Value, fallbackItems);
        FallbackCartStore.Clear(userId.Value);
        return Results.Ok(new { orderId = created.Id, total = created.TotalAmount, status = created.Status, mode = "fallback" });
    }
}).RequireAuthorization();

app.MapGet("/api/orders", async (HttpContext ctx) =>
{
    var userId = TryGetUserId(ctx);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        var orders = await conn.QueryAsync<OrderResponse>(
            @"SELECT Id, TotalAmount, Status, CreatedAt
              FROM Orders
              WHERE UserId = @userId
              ORDER BY Id DESC",
            new { userId });

        return Results.Ok(orders);
    }
    catch
    {
        return Results.Ok(FallbackOrderStore.GetOrders(userId.Value));
    }
}).RequireAuthorization();

app.MapGet("/api/refunds/my", (HttpContext ctx) =>
{
    var userId = TryGetUserId(ctx);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    return Results.Ok(RefundWorkflowStore.ListByUser(userId.Value));
}).RequireAuthorization();

app.MapPost("/api/refunds/request", async (HttpContext ctx, RefundRequest req) =>
{
    var userId = TryGetUserId(ctx);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    if (IsAdmin(ctx))
    {
        return Results.BadRequest(new { error = "Admin account cannot submit refund requests." });
    }

    if (req.OrderId <= 0 || string.IsNullOrWhiteSpace(req.Reason))
    {
        return Results.BadRequest(new { error = "OrderId and refund reason are required." });
    }

    var order = await FindOrderForUserAsync(req.OrderId, userId.Value);
    if (order is null || order.UserId != userId.Value)
    {
        return Results.NotFound(new { error = "Order not found." });
    }

    if (!string.Equals(order.Status, "Paid", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Only paid orders can be requested for refund." });
    }

    var username = TryGetUsername(ctx) ?? $"user{userId.Value}";
    var created = RefundWorkflowStore.Create(req.OrderId, userId.Value, username, req.Reason.Trim());
    if (!created.Success)
    {
        return Results.BadRequest(new { error = created.Error ?? "Refund request could not be created." });
    }

    return Results.Ok(created.Item);
}).RequireAuthorization();

app.MapGet("/api/admin/refunds", (HttpContext ctx) =>
{
    if (!IsAdmin(ctx))
    {
        return Results.Forbid();
    }

    return Results.Ok(RefundWorkflowStore.ListAll());
}).RequireAuthorization();

app.MapPost("/api/admin/refunds/{id:int}/decision", async (HttpContext ctx, int id, AdminRefundDecisionRequest req) =>
{
    if (!IsAdmin(ctx))
    {
        return Results.Forbid();
    }

    if (!req.Approve && string.IsNullOrWhiteSpace(req.Reason))
    {
        return Results.BadRequest(new { error = "Rejection reason is required." });
    }

    var adminUser = TryGetUsername(ctx) ?? "admin";
    var decision = RefundWorkflowStore.Decide(id, req.Approve, req.Reason?.Trim(), adminUser);
    if (!decision.Success || decision.Item is null)
    {
        return Results.BadRequest(new { error = decision.Error ?? "Refund decision failed." });
    }

    if (req.Approve)
    {
        await TryMarkOrderRefundedAsync(decision.Item.OrderId, decision.Item.UserId);
    }

    return Results.Ok(decision.Item);
}).RequireAuthorization();

app.MapGet("/api/orders/{orderId:int}/tracking", async (HttpContext ctx, int orderId) =>
{
    var userId = TryGetUserId(ctx);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    var order = await FindOrderForUserAsync(orderId, userId.Value);
    if (order is null || order.UserId != userId.Value)
    {
        return Results.NotFound(new { error = "Order not found." });
    }

    var refund = RefundWorkflowStore.GetLatestForOrder(userId.Value, orderId);

    var timeline = new List<string> { "Order placed" };
    string currentStatus;

    if (refund is not null)
    {
        timeline.Add("Refund requested");
        if (refund.Status == "Pending")
        {
            currentStatus = "Refund requested and under review";
            return Results.Ok(new TrackingResponse(orderId, $"DC-{userId.Value}-{orderId}", currentStatus, timeline, DateTime.UtcNow));
        }

        if (refund.Status == "Approved")
        {
            timeline.Add("Refund approved");
            timeline.Add("Amount returned to original payment method");
            currentStatus = "Refund completed";
            return Results.Ok(new TrackingResponse(orderId, $"DC-{userId.Value}-{orderId}", currentStatus, timeline, DateTime.UtcNow));
        }

        timeline.Add("Refund rejected");
        currentStatus = "Refund rejected";
        return Results.Ok(new TrackingResponse(orderId, $"DC-{userId.Value}-{orderId}", currentStatus, timeline, DateTime.UtcNow));
    }

    if (order.Status == "PendingPayment")
    {
        timeline.Add("Awaiting payment");
        currentStatus = "Waiting for payment";
        return Results.Ok(new TrackingResponse(orderId, $"DC-{userId.Value}-{orderId}", currentStatus, timeline, DateTime.UtcNow));
    }

    if (order.Status == "Refunded")
    {
        timeline.Add("Refund completed");
        currentStatus = "Refunded";
        return Results.Ok(new TrackingResponse(orderId, $"DC-{userId.Value}-{orderId}", currentStatus, timeline, DateTime.UtcNow));
    }

    if (order.Status == "Paid")
    {
        var age = DateTime.UtcNow - order.CreatedAt;
        var steps = new List<string>
        {
            "Payment confirmed",
            "Packed",
            "Shipped",
            "Out for delivery",
            "Delivered"
        };

        var stepIndex = age.TotalMinutes switch
        {
            < 10 => 0,
            < 30 => 1,
            < 120 => 2,
            < 360 => 3,
            _ => 4
        };

        for (var i = 0; i <= stepIndex; i++)
        {
            timeline.Add(steps[i]);
        }

        currentStatus = steps[stepIndex];
        return Results.Ok(new TrackingResponse(orderId, $"DC-{userId.Value}-{orderId}", currentStatus, timeline, DateTime.UtcNow));
    }

    timeline.Add(order.Status);
    currentStatus = order.Status;
    return Results.Ok(new TrackingResponse(orderId, $"DC-{userId.Value}-{orderId}", currentStatus, timeline, DateTime.UtcNow));
}).RequireAuthorization();

app.MapPost("/api/payments/pay", async (HttpContext ctx, PaymentRequest req) =>
{
    var userId = TryGetUserId(ctx);
    if (userId is null)
    {
        return Results.Unauthorized();
    }

    if (req.OrderId <= 0 || string.IsNullOrWhiteSpace(req.CardHolderName) || string.IsNullOrWhiteSpace(req.CardLast4))
    {
        return Results.BadRequest(new { error = "OrderId, CardHolderName and CardLast4 are required." });
    }

    if (req.CardLast4.Length != 4 || !req.CardLast4.All(char.IsDigit))
    {
        return Results.BadRequest(new { error = "CardLast4 must be exactly 4 digits." });
    }

    try
    {
        await using var conn = new SqlConnection(BuildConnectionString());
        await conn.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var order = await conn.QuerySingleOrDefaultAsync<OrderForPayment>(
            @"SELECT Id, TotalAmount, Status
              FROM Orders
              WHERE Id = @orderId AND UserId = @userId",
            new { orderId = req.OrderId, userId },
            tx);

        if (order is null)
        {
            await tx.RollbackAsync();

            // Checkout may have been created in fallback mode when DB was unavailable.
            // If DB comes back before payment, try fallback store before returning not found.
            var payFallback = FallbackOrderStore.Pay(userId.Value, req.OrderId);
            if (payFallback == FallbackPaymentResult.Success)
            {
                return Results.Ok(new { orderId = req.OrderId, status = "Paid", mode = "fallback" });
            }

            if (payFallback == FallbackPaymentResult.AlreadyPaid)
            {
                return Results.BadRequest(new { error = "Order is already paid." });
            }

            return Results.NotFound(new { error = "Order not found." });
        }

        if (order.Status == "Paid")
        {
            await tx.RollbackAsync();
            return Results.BadRequest(new { error = "Order is already paid." });
        }

        await conn.ExecuteAsync(
            @"INSERT INTO Payments(OrderId, Amount, CardHolderName, CardLast4, Status, PaidAt)
              VALUES(@orderId, @amount, @cardHolderName, @cardLast4, 'Succeeded', SYSUTCDATETIME())",
            new
            {
                orderId = req.OrderId,
                amount = order.TotalAmount,
                cardHolderName = req.CardHolderName.Trim(),
                cardLast4 = req.CardLast4
            },
            tx);

        await conn.ExecuteAsync(
            "UPDATE Orders SET Status = 'Paid' WHERE Id = @orderId",
            new { orderId = req.OrderId },
            tx);

        await tx.CommitAsync();
        return Results.Ok(new { orderId = req.OrderId, status = "Paid" });
    }
    catch
    {
        var pay = FallbackOrderStore.Pay(userId.Value, req.OrderId);
        if (pay == FallbackPaymentResult.NotFound)
        {
            return Results.NotFound(new { error = "Order not found." });
        }

        if (pay == FallbackPaymentResult.AlreadyPaid)
        {
            return Results.BadRequest(new { error = "Order is already paid." });
        }

        return Results.Ok(new { orderId = req.OrderId, status = "Paid", mode = "fallback" });
    }
}).RequireAuthorization();

app.MapPost("/api/chat/send", async (ChatRequest req, IHttpClientFactory httpClientFactory) =>
{
    if (string.IsNullOrWhiteSpace(req.Message))
    {
        return Results.BadRequest(new { error = "Message is required." });
    }

    var trimmedMessage = req.Message.Trim();
    if (LooksLikeProductRequest(trimmedMessage))
    {
        var created = FallbackCatalogStore.CreateFromRequest(trimmedMessage);
        var createdReply = $"Great choice. I found a matching option for you: {created.Name} at ${created.Price:F2}. Check it in Catalog.";
        return Results.Ok(new ChatResponse(createdReply, "catalog-builder", created));
    }

    var aiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
    if (!string.IsNullOrWhiteSpace(aiApiKey))
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", aiApiKey);

            var payload = new
            {
                model = "gpt-4o-mini",
                messages = new object[]
                {
                    new { role = "system", content = "You are Sam, a friendly Demo Cart sales representative. Greet users warmly, ask clarifying shopping questions, suggest relevant products from shoes/clothes/watch/electronics/system/hardware categories, guide checkout and payment, and speak in a natural human sales tone. Keep responses concise and practical." },
                    new { role = "user", content = trimmedMessage }
                },
                max_tokens = 220,
                temperature = 0.3
            };

            var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", payload);
            if (response.IsSuccessStatusCode)
            {
                var parsed = await response.Content.ReadFromJsonAsync<OpenAiChatResponse>();
                var first = parsed?.choices?.FirstOrDefault()?.message?.content;
                if (!string.IsNullOrWhiteSpace(first))
                {
                    return Results.Ok(new ChatResponse(first.Trim(), "openai"));
                }
            }
        }
        catch
        {
            // Fallback assistant is returned below.
        }
    }

    var text = trimmedMessage.ToLowerInvariant();
    var reply = text switch
    {
        var t when t.Contains("hello") || t.Contains("hi") =>
            "Hi, welcome to Demo Cart. I am Sam, your sales rep. What are you shopping for today: shoes, clothes, watches, electronics, systems, or hardware?",
        var t when t.Contains("login") =>
            "Please sign in with admin and Admin@123 first, then I can help you build your cart and complete checkout.",
        var t when t.Contains("cart") =>
            "Great choice. Open Catalog, add what you like, then go to Cart to review quantities and total before checkout.",
        var t when t.Contains("payment") =>
            "After checkout, open Payment and enter card holder name plus exactly 4 digits for card tail, for example 1234.",
        var t when t.Contains("readiness") =>
            "Readiness checks if API can serve traffic. In this app it validates SQL connectivity via /health/ready.",
        _ => "I can answer app usage, deployment, and troubleshooting questions. Ask about cart, payment, ingress, pods, or DB issues."
    };

    return Results.Ok(new ChatResponse(reply, "fallback", null));
});

app.Run();

static int? TryGetUserId(HttpContext ctx)
{
    var userIdStr =
        ctx.User.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
        ctx.User.FindFirstValue("sub") ??
        ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);

    return int.TryParse(userIdStr, out var userId) ? userId : null;
}

static bool IsAdmin(HttpContext ctx)
{
    return ctx.User.IsInRole("admin") ||
           string.Equals(ctx.User.FindFirstValue(ClaimTypes.Role), "admin", StringComparison.OrdinalIgnoreCase);
}

static string? TryGetUsername(HttpContext ctx)
{
    return ctx.User.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ??
           ctx.User.FindFirstValue(ClaimTypes.Name) ??
           ctx.User.Identity?.Name;
}

static bool LooksLikeProductRequest(string message)
{
    var text = message.ToLowerInvariant();
    if (!(text.Contains("need") || text.Contains("want") || text.Contains("looking for") || text.Contains("show") || text.Contains("create")))
    {
        return false;
    }

    return text.Contains("shoe") || text.Contains("cloth") || text.Contains("watch") || text.Contains("electronic") ||
           text.Contains("laptop") || text.Contains("system") || text.Contains("hardware") || text.Contains("keyboard") ||
           text.Contains("mouse") || text.Contains("tablet") || text.Contains("camera") || text.Contains("speaker");
}

static string BuildConnectionString()
{
    var host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
    var db = Environment.GetEnvironmentVariable("DB_NAME") ?? "KubeCartDb";
    var user = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
    var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "Your_password123";
    var trustServerCert = Environment.GetEnvironmentVariable("DB_TRUST_SERVER_CERT") ?? "true";

    var cs = new SqlConnectionStringBuilder
    {
        DataSource = $"{host},{port}",
        InitialCatalog = db,
        UserID = user,
        Password = password,
        Encrypt = true,
        TrustServerCertificate = bool.TryParse(trustServerCert, out var v) && v,
        ConnectTimeout = 5
    };

    return cs.ConnectionString;
}

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string Username, string Role);
public record CreateTodoRequest(string Title);
public record AddCartItemRequest(int ProductId, int Quantity);
public record PaymentRequest(int OrderId, string CardHolderName, string CardLast4);
public record ChatRequest(string Message);
public record UserRecord(int Id, string Username, string PasswordPlain);
public record TodoItem(int Id, int UserId, string Title, bool IsDone);
public record ProductItem(int Id, string Name, decimal Price, string? ImageUrl);
public record CartItemResponse(int Id, int ProductId, string ProductName, decimal Price, int Quantity, string? ImageUrl, decimal LineTotal);
public record CartCheckoutRow(int ProductId, int Quantity, decimal Price);
public record OrderResponse(int Id, decimal TotalAmount, string Status, DateTime CreatedAt);
public record OrderForPayment(int Id, decimal TotalAmount, string Status);
public record OrderLookupResult(int Id, int UserId, string Status, DateTime CreatedAt);
public record RefundRequest(int OrderId, string Reason);
public record AdminRefundDecisionRequest(bool Approve, string? Reason);
public record RefundTicketResponse(int Id, int OrderId, int UserId, string Username, string Status, string RequestReason, string? AdminReason, string? DecidedBy, DateTime RequestedAt, DateTime? DecidedAt);
public record RefundStoreResult(bool Success, string? Error, RefundTicketResponse? Item);
public record TrackingResponse(int OrderId, string TrackingCode, string CurrentStatus, IReadOnlyList<string> Timeline, DateTime UpdatedAt);
public record ChatResponse(string Reply, string Provider, ProductItem? CreatedProduct = null);

public static class FallbackCartStore
{
    private static readonly ConcurrentDictionary<int, List<CartItemResponse>> CartByUser = new();
    private static int _nextId = 1000;

    public static IReadOnlyList<CartItemResponse> GetItems(int userId)
    {
        if (!CartByUser.TryGetValue(userId, out var items))
        {
            return Array.Empty<CartItemResponse>();
        }

        lock (items)
        {
            return items.OrderByDescending(x => x.Id).ToList();
        }
    }

    public static void Add(int userId, ProductItem product, int quantity)
    {
        var items = CartByUser.GetOrAdd(userId, _ => new List<CartItemResponse>());
        lock (items)
        {
            var existing = items.FirstOrDefault(x => x.ProductId == product.Id);
            if (existing is null)
            {
                var id = Interlocked.Increment(ref _nextId);
                items.Add(new CartItemResponse(id, product.Id, product.Name, product.Price, quantity, product.ImageUrl, product.Price * quantity));
                return;
            }

            items.Remove(existing);
            var newQty = existing.Quantity + quantity;
            items.Add(existing with { Quantity = newQty, LineTotal = existing.Price * newQty });
        }
    }

    public static bool Remove(int userId, int cartItemId)
    {
        if (!CartByUser.TryGetValue(userId, out var items))
        {
            return false;
        }

        lock (items)
        {
            var item = items.FirstOrDefault(x => x.Id == cartItemId);
            if (item is null)
            {
                return false;
            }

            items.Remove(item);
            return true;
        }
    }

    public static void Clear(int userId)
    {
        if (!CartByUser.TryGetValue(userId, out var items))
        {
            return;
        }

        lock (items)
        {
            items.Clear();
        }
    }
}

public static class FallbackCatalogStore
{
    private static readonly List<ProductItem> Seed = new()
    {
        new(1, "Shoes - Running Sneakers", 79.99m, "https://loremflickr.com/600/400/running-shoes?lock=1"),
        new(2, "Shoes - Formal Leather", 119.00m, "https://loremflickr.com/600/400/formal-shoes?lock=2"),
        new(3, "Shoes - Hiking Boots", 139.50m, "https://loremflickr.com/600/400/hiking-boots?lock=3"),
        new(4, "Shoes - Basketball High Top", 109.99m, "https://loremflickr.com/600/400/basketball-shoes?lock=4"),
        new(5, "Shoes - Casual Slip-On", 49.90m, "https://loremflickr.com/600/400/casual-shoes?lock=5"),
        new(6, "Clothes - Men Cotton T-Shirt", 14.99m, "https://loremflickr.com/600/400/mens-tshirt?lock=6"),
        new(7, "Clothes - Women Summer Dress", 39.99m, "https://loremflickr.com/600/400/womens-dress?lock=7"),
        new(8, "Clothes - Hoodie Unisex", 34.50m, "https://loremflickr.com/600/400/hoodie?lock=8"),
        new(9, "Clothes - Denim Jacket", 59.00m, "https://loremflickr.com/600/400/denim-jacket?lock=9"),
        new(10, "Clothes - Sports Track Pants", 29.95m, "https://loremflickr.com/600/400/track-pants?lock=10"),
        new(11, "Watch - Smart Watch Pro", 199.00m, "https://loremflickr.com/600/400/smartwatch?lock=11"),
        new(12, "Watch - Analog Classic", 89.99m, "https://loremflickr.com/600/400/analog-watch?lock=12"),
        new(13, "Watch - Digital Sports", 59.00m, "https://loremflickr.com/600/400/digital-watch?lock=13"),
        new(14, "Watch - Luxury Steel", 249.90m, "https://loremflickr.com/600/400/luxury-watch?lock=14"),
        new(15, "Watch - Kids Color Watch", 24.50m, "https://loremflickr.com/600/400/kids-watch?lock=15"),
        new(16, "Electronics - Bluetooth Earbuds", 49.99m, "https://loremflickr.com/600/400/earbuds?lock=16"),
        new(17, "Electronics - 4K Action Camera", 129.00m, "https://loremflickr.com/600/400/action-camera?lock=17"),
        new(18, "Electronics - Portable Speaker", 69.99m, "https://loremflickr.com/600/400/bluetooth-speaker?lock=18"),
        new(19, "Electronics - Tablet 10-inch", 229.00m, "https://loremflickr.com/600/400/tablet-device?lock=19"),
        new(20, "Electronics - Power Bank 20000mAh", 39.99m, "https://loremflickr.com/600/400/power-bank?lock=20"),
        new(21, "System - Gaming Laptop", 1299.00m, "https://loremflickr.com/600/400/gaming-laptop?lock=21"),
        new(22, "System - Desktop PC i7", 999.00m, "https://loremflickr.com/600/400/desktop-computer?lock=22"),
        new(23, "System - Mini PC", 349.00m, "https://loremflickr.com/600/400/mini-pc?lock=23"),
        new(24, "System - All-in-One PC", 799.00m, "https://loremflickr.com/600/400/all-in-one-pc?lock=24"),
        new(25, "System - Chromebook", 299.00m, "https://loremflickr.com/600/400/chromebook?lock=25"),
        new(26, "Hardware - Mechanical Keyboard", 79.00m, "https://loremflickr.com/600/400/mechanical-keyboard?lock=26"),
        new(27, "Hardware - Wireless Mouse", 19.99m, "https://loremflickr.com/600/400/wireless-mouse?lock=27"),
        new(28, "Hardware - USB-C Hub", 34.50m, "https://loremflickr.com/600/400/usb-hub?lock=28"),
        new(29, "Hardware - NVMe SSD 1TB", 119.99m, "https://loremflickr.com/600/400/ssd-drive?lock=29"),
        new(30, "Hardware - DDR5 RAM 32GB Kit", 159.00m, "https://loremflickr.com/600/400/ram-memory?lock=30")
    };

    private static readonly List<ProductItem> Products = new(Seed);
    private static readonly object Sync = new();
    private static int _nextId = 10000;

    public static IReadOnlyList<ProductItem> GetAll()
    {
        lock (Sync)
        {
            return Products.OrderBy(x => x.Id).ToList();
        }
    }

    public static ProductItem CreateFromRequest(string request)
    {
        var clean = Regex.Replace(request.Trim(), "\\s+", " ");
        var lower = clean.ToLowerInvariant();

        var category = lower switch
        {
            var t when t.Contains("shoe") => "Shoes",
            var t when t.Contains("cloth") || t.Contains("shirt") || t.Contains("dress") || t.Contains("hoodie") => "Clothes",
            var t when t.Contains("watch") => "Watch",
            var t when t.Contains("electronic") || t.Contains("camera") || t.Contains("speaker") || t.Contains("tablet") => "Electronics",
            var t when t.Contains("laptop") || t.Contains("desktop") || t.Contains("system") || t.Contains("pc") => "System",
            _ => "Hardware"
        };

        var detail = clean.Length > 48 ? clean[..48] : clean;
        var name = $"{category} - Custom {detail}";
        var price = category switch
        {
            "Shoes" => 89.99m,
            "Clothes" => 34.99m,
            "Watch" => 149.00m,
            "Electronics" => 199.00m,
            "System" => 899.00m,
            _ => 79.00m
        };

        lock (Sync)
        {
            var existing = Products.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                return existing;
            }

            var created = new ProductItem(
                Interlocked.Increment(ref _nextId),
                name,
                price,
                "https://placehold.co/600x400/e5e7eb/1f2937?text=Custom+Product");

            Products.Add(created);
            return created;
        }
    }
}

public enum FallbackPaymentResult
{
    Success,
    NotFound,
    AlreadyPaid
}

public static class FallbackOrderStore
{
    private static readonly ConcurrentDictionary<int, List<OrderResponse>> OrdersByUser = new();
    private static int _nextOrderId = 5000;

    public static OrderResponse Create(int userId, IReadOnlyList<CartItemResponse> cartItems)
    {
        var total = cartItems.Sum(x => x.LineTotal);
        var order = new OrderResponse(
            Interlocked.Increment(ref _nextOrderId),
            total,
            "PendingPayment",
            DateTime.UtcNow);

        var orders = OrdersByUser.GetOrAdd(userId, _ => new List<OrderResponse>());
        lock (orders)
        {
            orders.Add(order);
        }

        return order;
    }

    public static IReadOnlyList<OrderResponse> GetOrders(int userId)
    {
        if (!OrdersByUser.TryGetValue(userId, out var orders))
        {
            return Array.Empty<OrderResponse>();
        }

        lock (orders)
        {
            return orders.OrderByDescending(x => x.Id).ToList();
        }
    }

    public static FallbackPaymentResult Pay(int userId, int orderId)
    {
        if (!OrdersByUser.TryGetValue(userId, out var orders))
        {
            return FallbackPaymentResult.NotFound;
        }

        lock (orders)
        {
            var idx = orders.FindIndex(x => x.Id == orderId);
            if (idx < 0)
            {
                return FallbackPaymentResult.NotFound;
            }

            if (orders[idx].Status == "Paid")
            {
                return FallbackPaymentResult.AlreadyPaid;
            }

            orders[idx] = orders[idx] with { Status = "Paid" };
            return FallbackPaymentResult.Success;
        }
    }

    public static OrderResponse? TryGetOrder(int userId, int orderId)
    {
        if (!OrdersByUser.TryGetValue(userId, out var orders))
        {
            return null;
        }

        lock (orders)
        {
            return orders.FirstOrDefault(x => x.Id == orderId);
        }
    }

    public static bool UpdateStatus(int userId, int orderId, string status)
    {
        if (!OrdersByUser.TryGetValue(userId, out var orders))
        {
            return false;
        }

        lock (orders)
        {
            var idx = orders.FindIndex(x => x.Id == orderId);
            if (idx < 0)
            {
                return false;
            }

            orders[idx] = orders[idx] with { Status = status };
            return true;
        }
    }
}

public static class RefundWorkflowStore
{
    private static readonly ConcurrentDictionary<int, RefundTicketResponse> Tickets = new();
    private static int _nextId = 9000;

    public static RefundStoreResult Create(int orderId, int userId, string username, string requestReason)
    {
        var active = Tickets.Values.FirstOrDefault(x =>
            x.OrderId == orderId &&
            x.UserId == userId &&
            (x.Status == "Pending" || x.Status == "Approved"));

        if (active is not null)
        {
            var error = active.Status == "Pending"
                ? "Refund request is already pending for this order."
                : "Order has already been refunded.";
            return new RefundStoreResult(false, error, active);
        }

        var id = Interlocked.Increment(ref _nextId);
        var created = new RefundTicketResponse(
            id,
            orderId,
            userId,
            username,
            "Pending",
            requestReason,
            null,
            null,
            DateTime.UtcNow,
            null);

        Tickets[id] = created;
        return new RefundStoreResult(true, null, created);
    }

    public static IReadOnlyList<RefundTicketResponse> ListByUser(int userId)
    {
        return Tickets.Values
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.RequestedAt)
            .ToList();
    }

    public static IReadOnlyList<RefundTicketResponse> ListAll()
    {
        return Tickets.Values
            .OrderByDescending(x => x.RequestedAt)
            .ToList();
    }

    public static RefundStoreResult Decide(int id, bool approve, string? reason, string decidedBy)
    {
        if (!Tickets.TryGetValue(id, out var existing))
        {
            return new RefundStoreResult(false, "Refund request not found.", null);
        }

        if (existing.Status != "Pending")
        {
            return new RefundStoreResult(false, "Refund request is already decided.", existing);
        }

        var status = approve ? "Approved" : "Rejected";
        var updated = existing with
        {
            Status = status,
            AdminReason = string.IsNullOrWhiteSpace(reason) ? null : reason,
            DecidedBy = decidedBy,
            DecidedAt = DateTime.UtcNow
        };

        Tickets[id] = updated;
        return new RefundStoreResult(true, null, updated);
    }

    public static RefundTicketResponse? GetLatestForOrder(int userId, int orderId)
    {
        return Tickets.Values
            .Where(x => x.UserId == userId && x.OrderId == orderId)
            .OrderByDescending(x => x.RequestedAt)
            .FirstOrDefault();
    }
}

public sealed class OpenAiChatResponse
{
    public List<OpenAiChoice>? choices { get; set; }
}

public sealed class OpenAiChoice
{
    public OpenAiMessage? message { get; set; }
}

public sealed class OpenAiMessage
{
    public string? content { get; set; }
}
