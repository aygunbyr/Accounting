# Accounting Backend (Clean Architecture)

## 🏗️ Project Overview
Kurumsal muhasebe yazılımı — Clean Architecture prensipleriyle tasarlanmış .NET 8 tabanlı backend.  
Katmanlar:
- **Api** – REST uçları (Swagger + ProblemDetails)
- **Application** – CQRS (MediatR), Validator, TransactionBehavior, ConcurrencyBehavior
- **Infrastructure** – EF Core (MSSQL), AuditSaveChangesInterceptor
- **Domain** – Entity ve enum tanımları

## ⚙️ Architecture
- **MediatR** + `IAppDbContext` (Repository yok, CQRS doğrudan context üstünden)
- **FluentValidation** (request validation)
- **ProblemDetails** – 400/404/409 standard responses
- **AuditSaveChangesInterceptor** – otomatik `CreatedAtUtc`/`UpdatedAtUtc`
- **Soft Delete** – `IsDeleted`, `DeletedAtUtc`, global filtre
- **PagedResult<T>** – tüm list endpoint’leri ortak DTO standardı
- **Money Helper** – `Money.S2/S3` string dönüşümleri, `AwayFromZero` rounding

## 📦 Phase‑1 Modules
| Modül | Özellikler |
|--------|-------------|
| **Contacts** | CRUD, SoftDelete, PagedResult |
| **Invoices** | List + filtre/sort, PagedTotals |
| **Payments** | List + filtre/sort (AccountId, ContactId, Direction, DateRange, **Currency**) |
| **Expenses** | Expense‑first flow (Draft → Reviewed → PostToBill) |
| **Items** | CRUD + Validation |
| **Cash/Bank Accounts** | CRUD, SoftDelete, RowVersion concurrency |

---

## 🔄 Optimistic Concurrency (RowVersion / 409)
_(docs/concurrency.md + genişletilmiş sürüm)_

### Amaç
Bir kaydı aynı anda birden fazla kullanıcının düzenlemesi durumunda veri kaybını önlemek.

### Mantık
- EF Core `IsRowVersion()` ile concurrency token.
- API `rowVersion` alanını **Base64** olarak döner.
- Update/Delete işlemleri bu token ile yapılır.
- Farklı değer gelirse `DbUpdateConcurrencyException` → `ConcurrencyConflictException` → 409.

### İstemci Akışı
1. `GET /{entity}/{id}` → `rowVersion` al.
2. `PUT/DELETE` → `rowVersion` gövdeye ekle.
3. 409 Conflict alırsan → yeniden `GET` et → yeni `rowVersion` ile tekrar gönder.

### Handler standardı (8 adım)
1. Fetch (tracking)  
2. İş kuralları  
3. OriginalValue = Base64 decode  
4. Normalize inputs  
5. UpdatedAtUtc (interceptor set eder)  
6. Save + catch concurrency  
7. Fresh read (AsNoTracking)  
8. DTO build (`rowVersion`, created/updated)

---

## 💰 Money & Decimal Policy
### Neden?
IEEE‑754 double hata payı yüzünden decimal zorunludur.

### Kurallar
- Depolama: `decimal(18,2)`  
- Hesaplama: backend’de, `MidpointRounding.AwayFromZero`
- DTO’larda tüm amount alanları string (`Money.S2`)
- FE sadece girdileri gönderir, ara hesaplamalar opsiyonel (`decimal.js`)
- ISO‑4217 Currency (3 harf)

### Örnek
```json
{
  "amount": "1500.00",
  "currency": "TRY",
  "totals": {
    "pageTotalAmount": "1500.00",
    "filteredTotalAmount": "3500.00"
  }
}
```

---

## 🧾 Expense‑first Flow
_(docs/expense-flow.md)_

```
Draft → Reviewed → Posted (Purchase Invoice)
   |        |→ Review        |→ PostToBill
```
1. **Draft:** masraf listesi oluşturulur, satırlar eklenebilir.  
2. **Reviewed:** onaylanır, artık düzenlenemez.  
3. **Posted:** PostToBill komutu ile satın alma faturasına dönüştürülür.  

Alternatif: **Bill‑first flow** (ileride).  
Tüm parasal hesaplamalar backend tarafında yapılır.

---

## 🌐 API Standards

- **PagedResult<T>** → `(total, pageNumber, pageSize, items, totals)`  
- **Sort formatı:** `"field:asc|desc"`  
- **Tarih:** UTC ISO‑8601 (`2025‑10‑13` veya `2025‑10‑13T10:00:00Z`)  
- **ProblemDetails:**  
  - 400 Validation, 404 NotFound, 409 ConcurrencyConflict  

**SoftDelete** → `IsDeleted=true`, listelerde `!IsDeleted` filtresi.  

---

## 🧪 Testing with Swagger

### Concurrency testi
1. `POST /api/cashbankaccounts`
2. `GET /api/cashbankaccounts/{id}` → rowVersion al
3. İki farklı tabda aynı kaydı düzenle
4. İlk PUT başarılı, ikinci PUT 409 Conflict

### Money/decimal doğrulaması
1. `POST /api/payments` (TRY & USD)
2. `GET /api/payments?currency=TRY` → yalnız TRY kayıtları
3. `GET /api/payments?currency=USD` → yalnız USD
4. `GET /api/payments` → karma toplam (sadece kontrol amaçlı)

---

## 🧩 Database & Index Recommendations

| Tablo | Önerilen Index | Amaç |
|--------|----------------|------|
| `Invoices` | `(DateUtc, ContactId, Currency)` | Liste filtreleri |
| `Payments` | `(DateUtc, AccountId, ContactId, Currency)` | Tarih + filtre sorguları |
| `Expenses` | `(Status)` | Draft/Reviewed filtreleri |
| `CashBankAccounts` | `(IsDeleted)` | SoftDelete performansı |

---

© 2025 Accounting Project – Clean Architecture Backend
