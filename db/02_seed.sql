USE KubeCartDb;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordPlain)
    VALUES ('admin', 'Admin@123');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'user1')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordPlain)
    VALUES ('user1', 'User@123');
END
GO

DECLARE @AdminId INT;
SELECT @AdminId = Id FROM dbo.Users WHERE Username = 'admin';

IF NOT EXISTS (SELECT 1 FROM dbo.Todos WHERE UserId = @AdminId)
BEGIN
    INSERT INTO dbo.Todos (UserId, Title, IsDone)
    VALUES
        (@AdminId, 'Review Kubernetes manifests', 0),
        (@AdminId, 'Verify external SQL connectivity', 1),
        (@AdminId, 'Capture demo screenshots', 0);
END
GO

DECLARE @Catalog TABLE (
    Name NVARCHAR(150) NOT NULL,
    Price DECIMAL(10,2) NOT NULL,
    ImageUrl NVARCHAR(400) NULL,
    IsActive BIT NOT NULL
);

INSERT INTO @Catalog (Name, Price, ImageUrl, IsActive)
VALUES
    ('Shoes - Running Sneakers', 79.99, 'https://picsum.photos/seed/shoes-running/300/200', 1),
    ('Shoes - Formal Leather', 119.00, 'https://picsum.photos/seed/shoes-formal/300/200', 1),
    ('Shoes - Hiking Boots', 139.50, 'https://picsum.photos/seed/shoes-hiking/300/200', 1),
    ('Shoes - Basketball High Top', 109.99, 'https://picsum.photos/seed/shoes-basketball/300/200', 1),
    ('Shoes - Casual Slip-On', 49.90, 'https://picsum.photos/seed/shoes-casual/300/200', 1),

    ('Clothes - Men Cotton T-Shirt', 14.99, 'https://picsum.photos/seed/clothes-men-tshirt/300/200', 1),
    ('Clothes - Women Summer Dress', 39.99, 'https://picsum.photos/seed/clothes-women-dress/300/200', 1),
    ('Clothes - Hoodie Unisex', 34.50, 'https://picsum.photos/seed/clothes-hoodie/300/200', 1),
    ('Clothes - Denim Jacket', 59.00, 'https://picsum.photos/seed/clothes-jacket/300/200', 1),
    ('Clothes - Sports Track Pants', 29.95, 'https://picsum.photos/seed/clothes-trackpants/300/200', 1),

    ('Watch - Smart Watch Pro', 199.00, 'https://picsum.photos/seed/watch-smart/300/200', 1),
    ('Watch - Analog Classic', 89.99, 'https://picsum.photos/seed/watch-analog/300/200', 1),
    ('Watch - Digital Sports', 59.00, 'https://picsum.photos/seed/watch-digital/300/200', 1),
    ('Watch - Luxury Steel', 249.90, 'https://picsum.photos/seed/watch-luxury/300/200', 1),
    ('Watch - Kids Color Watch', 24.50, 'https://picsum.photos/seed/watch-kids/300/200', 1),

    ('Electronics - Bluetooth Earbuds', 49.99, 'https://picsum.photos/seed/electronics-earbuds/300/200', 1),
    ('Electronics - 4K Action Camera', 129.00, 'https://picsum.photos/seed/electronics-camera/300/200', 1),
    ('Electronics - Portable Speaker', 69.99, 'https://picsum.photos/seed/electronics-speaker/300/200', 1),
    ('Electronics - Tablet 10-inch', 229.00, 'https://picsum.photos/seed/electronics-tablet/300/200', 1),
    ('Electronics - Power Bank 20000mAh', 39.99, 'https://picsum.photos/seed/electronics-powerbank/300/200', 1),

    ('System - Gaming Laptop', 1299.00, 'https://picsum.photos/seed/system-laptop/300/200', 1),
    ('System - Desktop PC i7', 999.00, 'https://picsum.photos/seed/system-desktop/300/200', 1),
    ('System - Mini PC', 349.00, 'https://picsum.photos/seed/system-minipc/300/200', 1),
    ('System - All-in-One PC', 799.00, 'https://picsum.photos/seed/system-aio/300/200', 1),
    ('System - Chromebook', 299.00, 'https://picsum.photos/seed/system-chromebook/300/200', 1),

    ('Hardware - Mechanical Keyboard', 79.00, 'https://picsum.photos/seed/hardware-keyboard/300/200', 1),
    ('Hardware - Wireless Mouse', 19.99, 'https://picsum.photos/seed/hardware-mouse/300/200', 1),
    ('Hardware - USB-C Hub', 34.50, 'https://picsum.photos/seed/hardware-hub/300/200', 1),
    ('Hardware - NVMe SSD 1TB', 119.99, 'https://picsum.photos/seed/hardware-ssd/300/200', 1),
    ('Hardware - DDR5 RAM 32GB Kit', 159.00, 'https://picsum.photos/seed/hardware-ram/300/200', 1),

    ('Home - Robot Vacuum Cleaner', 249.99, 'https://picsum.photos/seed/home-vacuum/300/200', 1),
    ('Home - Air Fryer XL', 99.00, 'https://picsum.photos/seed/home-airfryer/300/200', 1),
    ('Home - Espresso Coffee Machine', 189.00, 'https://picsum.photos/seed/home-coffee/300/200', 1),
    ('Home - Smart LED Lamp', 29.99, 'https://picsum.photos/seed/home-ledlamp/300/200', 1),
    ('Home - Memory Foam Pillow', 24.00, 'https://picsum.photos/seed/home-pillow/300/200', 1),

    ('Accessories - Laptop Sleeve', 22.99, 'https://picsum.photos/seed/accessories-sleeve/300/200', 1),
    ('Accessories - Phone Case', 14.50, 'https://picsum.photos/seed/accessories-case/300/200', 1),
    ('Accessories - Sunglasses UV400', 32.00, 'https://picsum.photos/seed/accessories-sunglasses/300/200', 1),
    ('Accessories - Travel Backpack', 54.00, 'https://picsum.photos/seed/accessories-backpack/300/200', 1),
    ('Accessories - Wallet Leather', 27.90, 'https://picsum.photos/seed/accessories-wallet/300/200', 1);

INSERT INTO dbo.Products (Name, Price, ImageUrl, IsActive)
SELECT c.Name, c.Price, c.ImageUrl, c.IsActive
FROM @Catalog c
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Products p
    WHERE p.Name = c.Name
);
GO
