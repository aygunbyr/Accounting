-- =============================================
-- Tüm tabloları temizler (development ortamı için)
-- Identity'ler sıfırlanır
-- FK sıralamasına dikkat edilmiştir
-- =============================================
USE AccountingDb

-- Önce child tablolar (FK sırasına göre)
DELETE FROM ExpenseLines
DELETE FROM ExpenseLists
DELETE FROM ExpenseDefinitions
DELETE FROM FixedAssets
DELETE FROM InvoiceLines
DELETE FROM Payments
DELETE FROM Cheques
DELETE FROM Invoices
DELETE FROM OrderLines
DELETE FROM Orders
DELETE FROM StockMovements
DELETE FROM Stocks
DELETE FROM Warehouses
DELETE FROM Items
DELETE FROM Categories
DELETE FROM CashBankAccounts
DELETE FROM Contacts
DELETE FROM Branches

-- Identity sıfırlama
DBCC CHECKIDENT ('ExpenseLines', RESEED, 0)
DBCC CHECKIDENT ('ExpenseLists', RESEED, 0)
DBCC CHECKIDENT ('ExpenseDefinitions', RESEED, 0)
DBCC CHECKIDENT ('FixedAssets', RESEED, 0)
DBCC CHECKIDENT ('InvoiceLines', RESEED, 0)
DBCC CHECKIDENT ('Payments', RESEED, 0)
DBCC CHECKIDENT ('Cheques', RESEED, 0)
DBCC CHECKIDENT ('Invoices', RESEED, 0)
DBCC CHECKIDENT ('OrderLines', RESEED, 0)
DBCC CHECKIDENT ('Orders', RESEED, 0)
DBCC CHECKIDENT ('StockMovements', RESEED, 0)
DBCC CHECKIDENT ('Stocks', RESEED, 0)
DBCC CHECKIDENT ('Warehouses', RESEED, 0)
DBCC CHECKIDENT ('Items', RESEED, 0)
DBCC CHECKIDENT ('Categories', RESEED, 0)
DBCC CHECKIDENT ('CashBankAccounts', RESEED, 0)
DBCC CHECKIDENT ('Contacts', RESEED, 0)
DBCC CHECKIDENT ('Branches', RESEED, 0)