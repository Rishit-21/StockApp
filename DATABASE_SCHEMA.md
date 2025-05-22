# Conceptual Database Schema (SQL DDL)

This document provides conceptual SQL Data Definition Language (DDL) examples for the tables corresponding to the C# domain models used in the StockTrader application. Note that Entity Framework Core (Code-First) is used to manage the actual database schema based on the C# models in `StockTrader.Core` and the `StockTraderDbContext` in `StockTrader.Data`.

These DDL examples are for illustrative purposes and might need adjustments based on the specific database (PostgreSQL or SQL Server) and EF Core conventions or fluent API configurations. EF Core migrations would typically generate more precise DDL.

## 1. Stocks Table

Corresponds to `StockTrader.Core.Models.Stock`.

**PostgreSQL Example:**
```sql
CREATE TABLE "Stocks" (
    "Id" SERIAL PRIMARY KEY,
    "Symbol" VARCHAR(50) NOT NULL UNIQUE,
    "Name" VARCHAR(255) NOT NULL,
    "Exchange" INTEGER NOT NULL -- Corresponds to Exchange enum (0: NSE, 1: BSE)
);

CREATE INDEX "IX_Stocks_Symbol" ON "Stocks" ("Symbol");
```

**SQL Server Example:**
```sql
CREATE TABLE "Stocks" (
    "Id" INT IDENTITY(1,1) PRIMARY KEY,
    "Symbol" NVARCHAR(50) NOT NULL UNIQUE,
    "Name" NVARCHAR(255) NOT NULL,
    "Exchange" INT NOT NULL -- Corresponds to Exchange enum (0: NSE, 1: BSE)
);

CREATE UNIQUE INDEX "IX_Stocks_Symbol" ON "Stocks" ("Symbol");
```

## 2. HistoricalPrices Table

Corresponds to `StockTrader.Core.Models.HistoricalPrice`.

**PostgreSQL Example:**
```sql
CREATE TABLE "HistoricalPrices" (
    "Id" SERIAL PRIMARY KEY,
    "StockId" INTEGER NOT NULL REFERENCES "Stocks"("Id") ON DELETE CASCADE,
    "Date" DATE NOT NULL,
    "Open" DECIMAL(18,2) NOT NULL,
    "High" DECIMAL(18,2) NOT NULL,
    "Low" DECIMAL(18,2) NOT NULL,
    "Close" DECIMAL(18,2) NOT NULL,
    "Volume" BIGINT NOT NULL
);

CREATE INDEX "IX_HistoricalPrices_StockId_Date" ON "HistoricalPrices" ("StockId", "Date" DESC);
```

**SQL Server Example:**
```sql
CREATE TABLE "HistoricalPrices" (
    "Id" INT IDENTITY(1,1) PRIMARY KEY,
    "StockId" INT NOT NULL FOREIGN KEY REFERENCES "Stocks"("Id") ON DELETE CASCADE,
    "Date" DATE NOT NULL,
    "Open" DECIMAL(18,2) NOT NULL,
    "High" DECIMAL(18,2) NOT NULL,
    "Low" DECIMAL(18,2) NOT NULL,
    "Close" DECIMAL(18,2) NOT NULL,
    "Volume" BIGINT NOT NULL
);

CREATE INDEX "IX_HistoricalPrices_StockId_Date" ON "HistoricalPrices" ("StockId", "Date" DESC);
```

## 3. LivePrices Table

Corresponds to `StockTrader.Core.Models.LivePrice`.

**PostgreSQL Example:**
```sql
CREATE TABLE "LivePrices" (
    "Id" SERIAL PRIMARY KEY,
    "StockId" INTEGER NOT NULL REFERENCES "Stocks"("Id") ON DELETE CASCADE,
    "Timestamp" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "Price" DECIMAL(18,2) NOT NULL,
    "Volume" BIGINT NOT NULL
);

CREATE INDEX "IX_LivePrices_StockId_Timestamp" ON "LivePrices" ("StockId", "Timestamp" DESC);
```

**SQL Server Example:**
```sql
CREATE TABLE "LivePrices" (
    "Id" INT IDENTITY(1,1) PRIMARY KEY,
    "StockId" INT NOT NULL FOREIGN KEY REFERENCES "Stocks"("Id") ON DELETE CASCADE,
    "Timestamp" DATETIME2 NOT NULL,
    "Price" DECIMAL(18,2) NOT NULL,
    "Volume" BIGINT NOT NULL
);

CREATE INDEX "IX_LivePrices_StockId_Timestamp" ON "LivePrices" ("StockId", "Timestamp" DESC);
```

## 4. SentimentData Table

Corresponds to `StockTrader.Core.Models.SentimentData`.

**PostgreSQL Example:**
```sql
CREATE TABLE "SentimentData" (
    "Id" SERIAL PRIMARY KEY,
    "Source" VARCHAR(100) NOT NULL,
    "Timestamp" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "Text" TEXT,
    "SentimentScore" DECIMAL(3,2) NOT NULL, -- e.g., score between -1.00 and 1.00
    "StockSymbol" VARCHAR(50) -- Can be NULL if sentiment is general
);

CREATE INDEX "IX_SentimentData_StockSymbol_Timestamp" ON "SentimentData" ("StockSymbol", "Timestamp" DESC);
CREATE INDEX "IX_SentimentData_Timestamp" ON "SentimentData" ("Timestamp" DESC);
```

**SQL Server Example:**
```sql
CREATE TABLE "SentimentData" (
    "Id" INT IDENTITY(1,1) PRIMARY KEY,
    "Source" NVARCHAR(100) NOT NULL,
    "Timestamp" DATETIME2 NOT NULL,
    "Text" NVARCHAR(MAX),
    "SentimentScore" DECIMAL(3,2) NOT NULL, -- e.g., score between -1.00 and 1.00
    "StockSymbol" NVARCHAR(50) -- Can be NULL if sentiment is general
);

CREATE INDEX "IX_SentimentData_StockSymbol_Timestamp" ON "SentimentData" ("StockSymbol", "Timestamp" DESC);
CREATE INDEX "IX_SentimentData_Timestamp" ON "SentimentData" ("Timestamp" DESC);
```

## 5. TradingTips Table

Corresponds to `StockTrader.Core.Models.TradingTip`.

**PostgreSQL Example:**
```sql
CREATE TABLE "TradingTips" (
    "Id" SERIAL PRIMARY KEY,
    "Timestamp" TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    "StockSymbol" VARCHAR(50) NOT NULL,
    "TipType" INTEGER NOT NULL, -- Corresponds to TipType enum (0: Intraday, 1: Options, 2: Swing)
    "Action" INTEGER NOT NULL,  -- Corresponds to ActionType enum (0: Buy, 1: Sell, 2: Hold)
    "Reason" TEXT,
    "ConfidenceScore" DECIMAL(3,2) -- e.g., score between 0.00 and 1.00
);

CREATE INDEX "IX_TradingTips_StockSymbol_Timestamp" ON "TradingTips" ("StockSymbol", "Timestamp" DESC);
CREATE INDEX "IX_TradingTips_Timestamp" ON "TradingTips" ("Timestamp" DESC);
```

**SQL Server Example:**
```sql
CREATE TABLE "TradingTips" (
    "Id" INT IDENTITY(1,1) PRIMARY KEY,
    "Timestamp" DATETIME2 NOT NULL,
    "StockSymbol" NVARCHAR(50) NOT NULL,
    "TipType" INT NOT NULL, -- Corresponds to TipType enum (0: Intraday, 1: Options, 2: Swing)
    "Action" INT NOT NULL,  -- Corresponds to ActionType enum (0: Buy, 1: Sell, 2: Hold)
    "Reason" NVARCHAR(MAX),
    "ConfidenceScore" DECIMAL(3,2) -- e.g., score between 0.00 and 1.00
);

CREATE INDEX "IX_TradingTips_StockSymbol_Timestamp" ON "TradingTips" ("StockSymbol", "Timestamp" DESC);
CREATE INDEX "IX_TradingTips_Timestamp" ON "TradingTips" ("Timestamp" DESC);
```
