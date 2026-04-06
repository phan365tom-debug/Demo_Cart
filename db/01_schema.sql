IF DB_ID('KubeCartDb') IS NULL
BEGIN
    CREATE DATABASE KubeCartDb;
END
GO

USE KubeCartDb;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordPlain NVARCHAR(255) NOT NULL
    );
END
GO

IF OBJECT_ID('dbo.Todos', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Todos (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        Title NVARCHAR(255) NOT NULL,
        IsDone BIT NOT NULL DEFAULT(0),
        CONSTRAINT FK_Todos_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
END
GO

IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(150) NOT NULL,
        Price DECIMAL(10,2) NOT NULL,
        ImageUrl NVARCHAR(400) NULL,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

IF OBJECT_ID('dbo.CartItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CartItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_CartItems_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id),
        CONSTRAINT UQ_Cart_User_Product UNIQUE (UserId, ProductId)
    );
END
GO

IF OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        TotalAmount DECIMAL(10,2) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
END
GO

IF OBJECT_ID('dbo.OrderItems', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OrderItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(10,2) NOT NULL,
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id),
        CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id)
    );
END
GO

IF OBJECT_ID('dbo.Payments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Payments (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        Amount DECIMAL(10,2) NOT NULL,
        CardHolderName NVARCHAR(150) NOT NULL,
        CardLast4 CHAR(4) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        PaidAt DATETIME2 NOT NULL,
        CONSTRAINT FK_Payments_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id)
    );
END
GO
