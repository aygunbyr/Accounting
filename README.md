# 📊 Accounting & Inventory Management System

**.NET 8** tabanlı kurumsal muhasebe ve stok yönetimi sistemi. **Clean Architecture**, **CQRS**, **Domain-Driven Design** prensipleriyle geliştirilmiştir.

---

## 🏗️ Mimari

### Katmanlar
```
├── Accounting.Api              # REST API endpoints (Controllers)
├── Accounting.Application      # CQRS (Commands/Queries), Business Logic
├── Accounting.Domain           # Entities, Enums, Value Objects
└── Accounting.Infrastructure   # EF Core, Persistence, External Services
```

### Temel Prensipler
- **CQRS (MediatR)**: Command/Query ayrımı
- **Clean Architecture**: Domain merkezli, bağımlılıklar içe doğru
- **Repository Pattern yok**: CQRS handler'lar direkt `IAppDbContext` kullanır
- **FluentValidation**: Request validation
- **Optimistic Concurrency**: RowVersion ile çakışma kontrolü
- **Soft Delete**: Kayıtlar fiziksel olarak silinmez
- **Audit Trail**: `CreatedAtUtc`, `UpdatedAtUtc` otomatik eklenir

---

## 📦 Domain Modülleri

### 1. **Contacts (Cariler)**
- **Tipler**: Customer (Müşteri), Vendor (Tedarikçi), Employee (Personel)
- **Özellikler**: CRUD, soft delete, pagination, filtering

### 2. **Items (Ürün/Hizmetler)**
- Stok ve hizmet yönetimi
- **Özellikler**: CRUD, kod/isim validasyonu

### 3. **Invoices (Faturalar)**
- **Tipler**: Sales (Satış), Purchase (Alış)
- **İlişkiler**: Contact, InvoiceLines
- **Hesaplamalar**: Net, KDV, Gross (backend'de yapılır)
- **Özellikler**: Balance tracking, payment linking

### 4. **Payments (Tahsilat/Tediye)**
- **Yönler**: In (Tahsilat), Out (Ödeme)
- **İlişkiler**: CashBankAccount, Contact, Invoice
- **Özellikler**: Multi-currency, date range filtering

### 5. **Expense Lists (Masraf Listeleri)**
- **Workflow**: Draft → Reviewed → Posted
- **Post to Bill**: Masraf listesini satın alma faturasına çevirir
- **Özellikler**: Line-based editing, approval system

### 6. **Stock Management (Stok Yönetimi)**
- **Warehouse**: Depo tanımları
- **Stock**: Anlık stok miktarları (Warehouse + Item bazında)
- **StockMovement**: Stok hareketleri
  - **Tipler**: PurchaseIn, SalesOut, AdjustmentIn, AdjustmentOut

### 7. **Cash/Bank Accounts (Kasa/Banka)**
- **Tipler**: Cash, Bank
- Tahsilat/tediye hesapları

### 8. **Fixed Assets (Demirbaşlar)**
- Sabit kıymet yönetimi (MVP'de henüz aktif değil)

---

## 🔄 Optimistic Concurrency

Her entity `RowVersion` (byte[]) içerir. Güncelleme/silme işlemlerinde concurrency kontrolü yapılır.

### Akış
1. **GET** `/api/invoices/5` → `rowVersion: "AAAAAAAAB9E="` döner
2. **PUT** `/api/invoices/5` → Body'de `rowVersion` gönder
3. Başka biri aynı kaydı değiştirdiyse → **409 Conflict**

### Handler Pattern
```csharp
// 1. Fetch with tracking
var entity = await _db.Entities.FirstOrDefaultAsync(x => x.Id == id);

// 2. Set OriginalValue
var originalBytes = Convert.FromBase64String(req.RowVersion);
_db.Entry(entity).Property(nameof(Entity.RowVersion)).OriginalValue = originalBytes;

// 3. Update properties
entity.Name = req.Name;
entity.UpdatedAtUtc = DateTime.UtcNow;

// 4. Save with concurrency check
try {
    await _db.SaveChangesAsync();
} catch (DbUpdateConcurrencyException) {
    throw new ConcurrencyConflictException("Record was modified by another user.");
}
```

---

## 💰 Money & Decimal Policy

### Neden Decimal?
IEEE-754 double'da yuvarlama hataları var. Para hesaplamalarında `decimal` zorunlu.

### Kurallar
- **Veritabanı**: `decimal(18,2)` veya `decimal(18,3)` (stok için)
- **DTO**: String olarak (`"1500.00"`)
- **Parsing**: `Money.TryParse2()` veya `Money.TryParse3()`
- **Formatting**: `Money.S2()` veya `Money.S3()`
- **Yuvarlama**: `MidpointRounding.AwayFromZero`

### Örnek
```json
{
  "amount": "1500.00",
  "currency": "TRY",
  "vatAmount": "270.00",
  "grossAmount": "1770.00"
}
```

**Frontend**: Hesaplamalar backend'de yapılır, frontend sadece gösterir.

---

## 📋 Expense Workflow

```
Draft → Reviewed → Posted
  │         │         │
  └─ Edit   └─ Lock   └─ Invoice Created
```

### Adımlar
1. **Draft**: Masraf listesi oluştur, satırlar ekle
2. **Review**: Onay → artık düzenlenemez
3. **Post to Bill**: Satın alma faturasına çevir
   - CreatePayment=true → Otomatik ödeme kaydı

### Endpoint Örneği
```bash
POST /api/expense-lists/5/post-to-bill
{
  "expenseListId": 5,
  "supplierId": 10,
  "itemId": 3,
  "currency": "TRY",
  "createPayment": true,
  "paymentAccountId": 2
}
```

---

## 📊 Stock Management Workflow

### Initial Setup
1. **Warehouse oluştur**: `POST /api/warehouses`
2. **Item tanımla**: `POST /api/items`

### Stock Hareketleri
```bash
# Alış (stok girişi)
POST /api/stock-movements
{
  "branchId": 1,
  "warehouseId": 1,
  "itemId": 5,
  "type": "PurchaseIn",
  "quantity": "100.000",
  "transactionDateUtc": "2025-01-04T10:00:00Z"
}

# Satış (stok çıkışı)
POST /api/stock-movements
{
  "type": "SalesOut",
  "quantity": "10.000"
}
```

### Stok Sorgulama
```bash
GET /api/stocks?warehouseId=1&itemId=5
```

**Constraint**: Stok negatif olamaz (DB check constraint)

---

## 🌐 API Standartları

### Pagination
```json
{
  "items": [...],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 20
}
```

### Sorting
```
?sort=createdAtUtc:desc
?sort=name:asc
```

### Date Format
**UTC ISO-8601**: `2025-01-04T10:00:00Z`

### Error Responses (ProblemDetails)
- **400** Validation Error
- **404** Not Found
- **409** Concurrency Conflict

---

## 🗄️ Database Schema

### Key Tables
| Table | Description | Key Columns |
|-------|-------------|-------------|
| `Contacts` | Müşteri/Tedarikçi/Personel | `Type`, `TaxNumber` |
| `Items` | Ürün/Hizmet | `Code`, `Name`, `UnitPrice` |
| `Invoices` | Faturalar | `Type`, `ContactId`, `TotalGross`, `Balance` |
| `InvoiceLines` | Fatura Kalemleri | `InvoiceId`, `ItemId`, `Qty`, `UnitPrice` |
| `Payments` | Tahsilat/Tediye | `Direction`, `AccountId`, `LinkedInvoiceId` |
| `ExpenseLists` | Masraf Listeleri | `Status`, `PostedInvoiceId` |
| `ExpenseLines` | Masraf Satırları | `ExpenseListId`, `Amount`, `VatRate` |
| `Warehouses` | Depolar | `BranchId`, `Code`, `IsDefault` |
| `Stocks` | Anlık Stok | `WarehouseId`, `ItemId`, `Quantity` |
| `StockMovements` | Stok Hareketleri | `Type`, `Quantity`, `TransactionDateUtc` |

### Indexes
```sql
-- Performance için önerilen indexler
CREATE INDEX IX_Invoices_DateUtc_ContactId ON Invoices(DateUtc, ContactId);
CREATE INDEX IX_Payments_DateUtc_AccountId ON Payments(DateUtc, AccountId);
CREATE INDEX IX_Stocks_WarehouseId_ItemId ON Stocks(WarehouseId, ItemId);
CREATE UNIQUE INDEX UX_Stocks_Branch_Warehouse_Item ON Stocks(BranchId, WarehouseId, ItemId) WHERE IsDeleted = 0;
```

---

## 🧪 Testing Scenarios

### 1. Invoice + Payment Flow
```bash
# 1. Create sales invoice
POST /api/invoices { type: "Sales", contactId: 5, lines: [...] }
# Response: { id: 100, totalGross: "1770.00", balance: "1770.00" }

# 2. Create payment
POST /api/payments { 
  linkedInvoiceId: 100, 
  amount: "1770.00", 
  direction: "In" 
}
# Response: Invoice balance = 0

# 3. Verify balance
GET /api/invoices/100
# Response: { balance: "0.00" }
```

### 2. Expense Post to Bill
```bash
# 1. Create expense list
POST /api/expense-lists { name: "Ocak Masrafları", lines: [...] }

# 2. Review
POST /api/expense-lists/1/review

# 3. Post to bill with payment
POST /api/expense-lists/1/post-to-bill {
  supplierId: 10,
  itemId: 3,
  currency: "TRY",
  createPayment: true,
  paymentAccountId: 2
}
# Response: { createdInvoiceId: 101, postedExpenseCount: 5 }
```

### 3. Stock Movement
```bash
# 1. Create warehouse
POST /api/warehouses { branchId: 1, code: "W01", name: "Ana Depo" }

# 2. Purchase (stock in)
POST /api/stock-movements {
  warehouseId: 1,
  itemId: 5,
  type: "PurchaseIn",
  quantity: "100.000"
}

# 3. Check stock
GET /api/stocks?warehouseId=1&itemId=5
# Response: { quantity: "100.000" }

# 4. Sales (stock out)
POST /api/stock-movements {
  warehouseId: 1,
  itemId: 5,
  type: "SalesOut",
  quantity: "10.000"
}

# 5. Verify
GET /api/stocks?warehouseId=1&itemId=5
# Response: { quantity: "90.000" }
```

### 4. Concurrency Test
```bash
# 1. Get record
GET /api/invoices/100
# Response: { rowVersion: "AAAAAAAAB9E=" }

# 2. Two users try to update
# User A:
PUT /api/invoices/100 { name: "Updated A", rowVersion: "AAAAAAAAB9E=" }
# Success: 200 OK

# User B (same rowVersion):
PUT /api/invoices/100 { name: "Updated B", rowVersion: "AAAAAAAAB9E=" }
# Fail: 409 Conflict
```

---

## 🚀 Running the Project

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB or Express)

### Setup
```bash
# 1. Restore packages
dotnet restore

# 2. Update connection string (appsettings.json)
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AccountingDb;..."
}

# 3. Run migrations
dotnet ef database update --project Accounting.Infrastructure

# 4. Run API
dotnet run --project Accounting.Api
```

### Swagger
```
https://localhost:5001/swagger
```

---

## 📁 Project Structure

```
Accounting.Api/
├── Controllers/
│   ├── ContactsController.cs
│   ├── InvoicesController.cs
│   ├── PaymentsController.cs
│   ├── ExpenseListsController.cs
│   ├── StocksController.cs
│   └── ...
└── Program.cs

Accounting.Application/
├── Contacts/
│   ├── Commands/ (Create, Update, Delete)
│   └── Queries/ (GetById, List)
├── Invoices/
├── Payments/
├── ExpenseLists/
├── Stocks/
├── Warehouses/
└── Common/
    ├── Abstractions/ (IAppDbContext)
    ├── Behaviors/ (Validation, Transaction)
    ├── Errors/ (Exceptions)
    └── Utils/ (Money, PagedResult)

Accounting.Domain/
├── Entities/
│   ├── Contact.cs
│   ├── Invoice.cs
│   ├── Stock.cs
│   └── ...
├── Enums/
│   ├── ContactType.cs
│   ├── InvoiceType.cs
│   └── StockMovementType.cs
└── Common/ (Interfaces)

Accounting.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/ (Entity configurations)
│   └── Seed/ (DataSeeder)
└── Interceptors/ (AuditSaveChangesInterceptor)
```

---

## 🎯 Next Steps (Future Features)

- [ ] Invoice → Stock integration (otomatik stok hareketi)
- [ ] Multi-branch stock transfer
- [ ] Fixed Asset depreciation calculation
- [ ] Reporting module (balance sheet, P&L)
- [ ] User authentication & authorization
- [ ] Audit log tracking
- [ ] Email notifications

---

## 📝 Notes

### Enums Namespace
Tüm enum'lar `Accounting.Domain.Enums` namespace'inde toplanmıştır:
- ContactType
- InvoiceType
- PaymentDirection
- ExpenseListStatus
- StockMovementType
- CashBankAccountType

### Entity Naming
- `ExpenseLine` (eski adı: Expense)
- `InvoiceLine` (fatura kalemi)
- Tüm liste entity'leri çoğul: `ExpenseLists`, `Invoices`, `Stocks`

---

**© 2026 Accounting & Inventory Management System**  
Clean Architecture + CQRS + DDD
