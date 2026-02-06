-- =============================================
-- Site Yönetim Sistemi - Tam Veritabanı Şeması
-- Tüm tablolar, indeksler, migration'lar tek script'te
-- SQL Server Management Studio veya sqlcmd ile çalıştırın
-- =============================================

USE master;
GO
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SiteYonetim')
    CREATE DATABASE SiteYonetim COLLATE Turkish_CI_AS;
GO
USE SiteYonetim;
GO

-- ==================== TABLOLAR ====================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Sites')
CREATE TABLE dbo.Sites (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Address NVARCHAR(500) NULL,
    City NVARCHAR(100) NULL,
    District NVARCHAR(100) NULL,
    TaxOffice NVARCHAR(100) NULL,
    TaxNumber NVARCHAR(20) NULL,
    LateFeeRate DECIMAL(5,2) NULL,
    LateFeeDay INT NULL,
    HasMultipleBlocks BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Buildings')
CREATE TABLE dbo.Buildings (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    FloorCount INT NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Buildings_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Apartments')
CREATE TABLE dbo.Apartments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    BuildingId UNIQUEIDENTIFIER NULL,
    BlockOrBuildingName NVARCHAR(100) NOT NULL DEFAULT '',
    ApartmentNumber NVARCHAR(50) NOT NULL,
    Floor INT NULL,
    ShareRate DECIMAL(10,4) NOT NULL DEFAULT 1,
    OwnerName NVARCHAR(200) NULL,
    OwnerPhone NVARCHAR(50) NULL,
    OwnerEmail NVARCHAR(256) NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Apartments_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
    CONSTRAINT FK_Apartments_Building FOREIGN KEY (BuildingId) REFERENCES dbo.Buildings(Id),
    CONSTRAINT UQ_Apartments_Site_Block_Number UNIQUE (SiteId, BlockOrBuildingName, ApartmentNumber)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
CREATE TABLE dbo.Users (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    Email NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NULL,
    Role INT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1,
    RefreshToken NVARCHAR(500) NULL,
    RefreshTokenExpiry DATETIME2(2) NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserSites')
CREATE TABLE dbo.UserSites (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    SiteId UNIQUEIDENTIFIER NOT NULL,
    IsPrimary BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_UserSites_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_UserSites_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Residents')
CREATE TABLE dbo.Residents (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ApartmentId UNIQUEIDENTIFIER NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NULL,
    Email NVARCHAR(256) NULL,
    Type INT NOT NULL DEFAULT 0,
    IsOwner BIT NOT NULL DEFAULT 1,
    IdentityNumber NVARCHAR(20) NULL,
    MoveInDate DATE NULL,
    MoveOutDate DATE NULL,
    UserId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Residents_Apartment FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(Id),
    CONSTRAINT FK_Residents_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseTypes')
CREATE TABLE dbo.ExpenseTypes (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    ShareType INT NOT NULL DEFAULT 0,
    IsRecurring BIT NOT NULL DEFAULT 0,
    RecurringDayOfMonth INT NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ExpenseTypes_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expenses')
CREATE TABLE dbo.Expenses (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    ExpenseTypeId UNIQUEIDENTIFIER NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    ExpenseDate DATE NOT NULL,
    DueDate DATE NULL,
    Status INT NOT NULL DEFAULT 0,
    MeterReadingId UNIQUEIDENTIFIER NULL,
    InvoiceNumber NVARCHAR(50) NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Expenses_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
    CONSTRAINT FK_Expenses_ExpenseType FOREIGN KEY (ExpenseTypeId) REFERENCES dbo.ExpenseTypes(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Meters')
CREATE TABLE dbo.Meters (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    ApartmentId UNIQUEIDENTIFIER NULL,
    Name NVARCHAR(100) NOT NULL,
    Type NVARCHAR(50) NOT NULL DEFAULT N'Su',
    SerialNumber NVARCHAR(50) NULL,
    Unit NVARCHAR(20) NULL,
    Multiplier DECIMAL(10,4) NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Meters_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
    CONSTRAINT FK_Meters_Apartment FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MeterReadings')
CREATE TABLE dbo.MeterReadings (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    MeterId UNIQUEIDENTIFIER NOT NULL,
    ReadingValue DECIMAL(18,4) NOT NULL,
    ReadingDate DATE NOT NULL,
    PreviousReadingValue DECIMAL(18,4) NULL,
    Notes NVARCHAR(500) NULL,
    IsEstimated BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_MeterReadings_Meter FOREIGN KEY (MeterId) REFERENCES dbo.Meters(Id)
);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Expenses_MeterReading')
ALTER TABLE dbo.Expenses ADD CONSTRAINT FK_Expenses_MeterReading FOREIGN KEY (MeterReadingId) REFERENCES dbo.MeterReadings(Id);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseShares')
CREATE TABLE dbo.ExpenseShares (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ExpenseId UNIQUEIDENTIFIER NOT NULL,
    ApartmentId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    LateFeeAmount DECIMAL(18,2) NULL,
    PaidAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status INT NOT NULL DEFAULT 0,
    DueDate DATE NULL,
    Notes NVARCHAR(500) NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ExpenseShares_Expense FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(Id),
    CONSTRAINT FK_ExpenseShares_Apartment FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(Id),
    CONSTRAINT UQ_ExpenseShares_Expense_Apartment UNIQUE (ExpenseId, ApartmentId)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ExpenseAttachments')
CREATE TABLE dbo.ExpenseAttachments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    ExpenseId UNIQUEIDENTIFIER NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ExpenseAttachments_Expense FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BankAccounts')
CREATE TABLE dbo.BankAccounts (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    AccountType INT NOT NULL DEFAULT 0,
    BankName NVARCHAR(100) NOT NULL,
    BranchName NVARCHAR(100) NOT NULL,
    AccountNumber NVARCHAR(50) NOT NULL,
    IBAN NVARCHAR(34) NOT NULL,
    AccountName NVARCHAR(200) NULL,
    Currency NVARCHAR(3) NOT NULL DEFAULT 'TRY',
    IsDefault BIT NOT NULL DEFAULT 0,
    CurrentBalance DECIMAL(18,2) NOT NULL DEFAULT 0,
    OpeningBalance DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_BankAccounts_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
CREATE TABLE dbo.Payments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    ApartmentId UNIQUEIDENTIFIER NOT NULL,
    ExpenseShareId UNIQUEIDENTIFIER NULL,
    IncomeId UNIQUEIDENTIFIER NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentDate DATETIME2(2) NOT NULL,
    Method INT NOT NULL DEFAULT 0,
    ReferenceNumber NVARCHAR(100) NULL,
    Description NVARCHAR(500) NULL,
    BankAccountId UNIQUEIDENTIFIER NULL,
    ReceiptId UNIQUEIDENTIFIER NULL,
    CreditCardLastFour NVARCHAR(4) NULL,
    InstallmentCount INT NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Payments_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
    CONSTRAINT FK_Payments_Apartment FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(Id),
    CONSTRAINT FK_Payments_ExpenseShare FOREIGN KEY (ExpenseShareId) REFERENCES dbo.ExpenseShares(Id),
    CONSTRAINT FK_Payments_BankAccount FOREIGN KEY (BankAccountId) REFERENCES dbo.BankAccounts(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Receipts')
CREATE TABLE dbo.Receipts (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    PaymentId UNIQUEIDENTIFIER NOT NULL,
    ReceiptNumber NVARCHAR(50) NOT NULL,
    ReceiptDate DATE NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(500) NULL,
    PdfPath NVARCHAR(500) NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Receipts_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
    CONSTRAINT FK_Receipts_Payment FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(Id),
    CONSTRAINT UQ_Receipts_Site_Number UNIQUE (SiteId, ReceiptNumber)
);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Payments_Receipt')
ALTER TABLE dbo.Payments ADD CONSTRAINT FK_Payments_Receipt FOREIGN KEY (ReceiptId) REFERENCES dbo.Receipts(Id);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Incomes')
CREATE TABLE dbo.Incomes (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    ApartmentId UNIQUEIDENTIFIER NOT NULL,
    [Year] INT NOT NULL,
    [Month] INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    [Type] INT NOT NULL DEFAULT 0,
    Status INT NOT NULL DEFAULT 0,
    PaymentId UNIQUEIDENTIFIER NULL,
    DueDate DATE NOT NULL,
    PaymentStartDate DATE NOT NULL DEFAULT '2020-01-01',
    PaymentEndDate DATE NOT NULL DEFAULT '2020-01-31',
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Incomes_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
    CONSTRAINT FK_Incomes_Apartment FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(Id)
);

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Payments_Income')
ALTER TABLE dbo.Payments ADD CONSTRAINT FK_Payments_Income FOREIGN KEY (IncomeId) REFERENCES dbo.Incomes(Id);
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Incomes_Payment')
ALTER TABLE dbo.Incomes ADD CONSTRAINT FK_Incomes_Payment FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(Id);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RecurringCharges')
CREATE TABLE dbo.RecurringCharges (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    ApartmentId UNIQUEIDENTIFIER NOT NULL,
    ExpenseTypeId UNIQUEIDENTIFIER NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    DayOfMonth INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    StartDate DATE NULL,
    EndDate DATE NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_RecurringCharges_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
    CONSTRAINT FK_RecurringCharges_Apartment FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(Id),
    CONSTRAINT FK_RecurringCharges_ExpenseType FOREIGN KEY (ExpenseTypeId) REFERENCES dbo.ExpenseTypes(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'BankTransactions')
CREATE TABLE dbo.BankTransactions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    BankAccountId UNIQUEIDENTIFIER NOT NULL,
    TransactionDate DATETIME2(2) NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    Type INT NOT NULL DEFAULT 0,
    Description NVARCHAR(500) NULL,
    ReferenceNumber NVARCHAR(100) NULL,
    PaymentId UNIQUEIDENTIFIER NULL,
    ExpenseId UNIQUEIDENTIFIER NULL,
    BalanceAfter DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_BankTransactions_BankAccount FOREIGN KEY (BankAccountId) REFERENCES dbo.BankAccounts(Id),
    CONSTRAINT FK_BankTransactions_Payment FOREIGN KEY (PaymentId) REFERENCES dbo.Payments(Id),
    CONSTRAINT FK_BankTransactions_Expense FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupportTickets')
CREATE TABLE dbo.SupportTickets (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    ApartmentId UNIQUEIDENTIFIER NULL,
    ResidentId UNIQUEIDENTIFIER NULL,
    Subject NVARCHAR(300) NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    Priority INT NOT NULL DEFAULT 1,
    AssignedToUserId UNIQUEIDENTIFIER NULL,
    ClosedAt DATETIME2(2) NULL,
    Response NVARCHAR(MAX) NULL,
    ContactName NVARCHAR(200) NULL,
    ContactPhone NVARCHAR(50) NULL,
    TopicType INT NOT NULL DEFAULT 0,
    BlockNumber NVARCHAR(50) NULL,
    FloorNumber INT NULL,
    CreatedByUserId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SupportTickets_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
    CONSTRAINT FK_SupportTickets_Apartment FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(Id),
    CONSTRAINT FK_SupportTickets_Resident FOREIGN KEY (ResidentId) REFERENCES dbo.Residents(Id),
    CONSTRAINT FK_SupportTickets_AssignedUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupportTicketMessages')
CREATE TABLE dbo.SupportTicketMessages (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SupportTicketId UNIQUEIDENTIFIER NOT NULL,
    UserId UNIQUEIDENTIFIER NULL,
    IsFromResident BIT NOT NULL,
    Message NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SupportTicketMessages_Ticket FOREIGN KEY (SupportTicketId) REFERENCES dbo.SupportTickets(Id),
    CONSTRAINT FK_SupportTicketMessages_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupportTicketAttachments')
CREATE TABLE dbo.SupportTicketAttachments (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SupportTicketId UNIQUEIDENTIFIER NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SupportTicketAttachments_Ticket FOREIGN KEY (SupportTicketId) REFERENCES dbo.SupportTickets(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Surveys')
CREATE TABLE dbo.Surveys (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(300) NOT NULL,
    Description NVARCHAR(1000) NULL,
    StartDate DATETIME2(2) NOT NULL,
    EndDate DATETIME2(2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Surveys_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SurveyQuestions')
CREATE TABLE dbo.SurveyQuestions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SurveyId UNIQUEIDENTIFIER NOT NULL,
    QuestionText NVARCHAR(1000) NOT NULL,
    [Order] INT NOT NULL DEFAULT 0,
    IsMultipleChoice BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SurveyQuestions_Survey FOREIGN KEY (SurveyId) REFERENCES dbo.Surveys(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SurveyOptions')
CREATE TABLE dbo.SurveyOptions (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SurveyQuestionId UNIQUEIDENTIFIER NOT NULL,
    OptionText NVARCHAR(500) NOT NULL,
    [Order] INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SurveyOptions_Question FOREIGN KEY (SurveyQuestionId) REFERENCES dbo.SurveyQuestions(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SurveyVotes')
CREATE TABLE dbo.SurveyVotes (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SurveyId UNIQUEIDENTIFIER NOT NULL,
    SurveyOptionId UNIQUEIDENTIFIER NOT NULL,
    ApartmentId UNIQUEIDENTIFIER NOT NULL,
    ResidentId UNIQUEIDENTIFIER NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_SurveyVotes_Survey FOREIGN KEY (SurveyId) REFERENCES dbo.Surveys(Id),
    CONSTRAINT FK_SurveyVotes_Option FOREIGN KEY (SurveyOptionId) REFERENCES dbo.SurveyOptions(Id),
    CONSTRAINT FK_SurveyVotes_Apartment FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(Id),
    CONSTRAINT FK_SurveyVotes_Resident FOREIGN KEY (ResidentId) REFERENCES dbo.Residents(Id)
);

-- ==================== MİGRATİON ALANLARI ====================

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Sites') AND name = 'DefaultMonthlyDues')
    ALTER TABLE dbo.Sites ADD DefaultMonthlyDues DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Sites') AND name = 'DefaultPaymentStartDay')
    ALTER TABLE dbo.Sites ADD DefaultPaymentStartDay INT NOT NULL DEFAULT 1;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Sites') AND name = 'DefaultPaymentEndDay')
    ALTER TABLE dbo.Sites ADD DefaultPaymentEndDay INT NOT NULL DEFAULT 20;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Apartments') AND name = 'MonthlyDuesAmount')
    ALTER TABLE dbo.Apartments ADD MonthlyDuesAmount DECIMAL(18,2) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Apartments') AND name = 'PaymentStartDay')
    ALTER TABLE dbo.Apartments ADD PaymentStartDay INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Apartments') AND name = 'PaymentEndDay')
    ALTER TABLE dbo.Apartments ADD PaymentEndDay INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Expenses') AND name = 'InvoiceDate')
    ALTER TABLE dbo.Expenses ADD InvoiceDate DATE NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ExpenseTypes') AND name = 'ExcludeFromReport')
    ALTER TABLE dbo.ExpenseTypes ADD ExcludeFromReport BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'IsApproved')
    ALTER TABLE dbo.Users ADD IsApproved BIT NOT NULL DEFAULT 1;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Sites') AND name = 'SupportNotificationEmail')
    ALTER TABLE dbo.Sites ADD SupportNotificationEmail NVARCHAR(256) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Sites') AND name = 'SupportSmtpHost')
    ALTER TABLE dbo.Sites ADD SupportSmtpHost NVARCHAR(200) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Sites') AND name = 'SupportSmtpPort')
    ALTER TABLE dbo.Sites ADD SupportSmtpPort INT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Sites') AND name = 'SupportSmtpUsername')
    ALTER TABLE dbo.Sites ADD SupportSmtpUsername NVARCHAR(256) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Sites') AND name = 'SupportSmtpPassword')
    ALTER TABLE dbo.Sites ADD SupportSmtpPassword NVARCHAR(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Apartments') AND name = 'OccupancyType')
    ALTER TABLE dbo.Apartments ADD OccupancyType INT NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Apartments') AND name = 'TenantName')
    ALTER TABLE dbo.Apartments ADD TenantName NVARCHAR(200) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Apartments') AND name = 'TenantPhone')
    ALTER TABLE dbo.Apartments ADD TenantPhone NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SupportTickets') AND name = 'ContactEmail')
    ALTER TABLE dbo.SupportTickets ADD ContactEmail NVARCHAR(256) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.SupportTickets') AND name = 'ApartmentNumber')
    ALTER TABLE dbo.SupportTickets ADD ApartmentNumber NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Quotations')
CREATE TABLE dbo.Quotations (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    CompanyName NVARCHAR(200) NOT NULL,
    QuotationDate DATE NOT NULL,
    FilePath NVARCHAR(500) NULL,
    MonthlyFee DECIMAL(18,2) NULL,
    YearlyFee DECIMAL(18,2) NULL,
    Description NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Quotations_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ResidentContacts')
CREATE TABLE dbo.ResidentContacts (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    ApartmentId UNIQUEIDENTIFIER NULL,
    Name NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NOT NULL,
    ContactType INT NOT NULL DEFAULT 0,
    Notes NVARCHAR(500) NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ResidentContacts_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
    CONSTRAINT FK_ResidentContacts_Apartment FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(Id)
);

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ImportantPhones')
CREATE TABLE dbo.ImportantPhones (
    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    SiteId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Phone NVARCHAR(50) NOT NULL,
    ExtraInfo NVARCHAR(500) NULL,
    CreatedAt DATETIME2(2) NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2(2) NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ImportantPhones_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id)
);

-- BankAccounts CurrentBalance/OpeningBalance (zaten tablo tanımında)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BankAccounts') AND name = 'CurrentBalance')
    ALTER TABLE dbo.BankAccounts ADD CurrentBalance DECIMAL(18,2) NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BankAccounts') AND name = 'OpeningBalance')
    ALTER TABLE dbo.BankAccounts ADD OpeningBalance DECIMAL(18,2) NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.BankAccounts') AND name = 'AccountType')
    ALTER TABLE dbo.BankAccounts ADD AccountType INT NOT NULL DEFAULT 0;

-- Meters tablosu zaten Type NVARCHAR ile oluşturuluyor (satır 157). Eski INT migration kaldırıldı.

-- ==================== APARTMENTS UNIQUE CONSTRAINT MİGRATION ====================
-- Eski: (SiteId, ApartmentNumber) - B Blok'ta A Blok ile aynı daire no kullanılamıyordu
-- Yeni: (SiteId, BlockOrBuildingName, ApartmentNumber) - A Blok 1, B Blok 1 ayrı kaydedilebilir
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Apartments_Site_Number' AND object_id = OBJECT_ID('dbo.Apartments'))
    ALTER TABLE dbo.Apartments DROP CONSTRAINT UQ_Apartments_Site_Number;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Apartments_Site_Block_Number' AND object_id = OBJECT_ID('dbo.Apartments'))
    ALTER TABLE dbo.Apartments ADD CONSTRAINT UQ_Apartments_Site_Block_Number UNIQUE (SiteId, BlockOrBuildingName, ApartmentNumber);

-- ==================== İNDEKSLER ====================

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sites_Name' AND object_id = OBJECT_ID('dbo.Sites'))
    CREATE NONCLUSTERED INDEX IX_Sites_Name ON dbo.Sites(Name) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Apartments_SiteId' AND object_id = OBJECT_ID('dbo.Apartments'))
    CREATE NONCLUSTERED INDEX IX_Apartments_SiteId ON dbo.Apartments(SiteId) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserSites_UserId' AND object_id = OBJECT_ID('dbo.UserSites'))
    CREATE NONCLUSTERED INDEX IX_UserSites_UserId ON dbo.UserSites(UserId) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_UserSites_SiteId' AND object_id = OBJECT_ID('dbo.UserSites'))
    CREATE NONCLUSTERED INDEX IX_UserSites_SiteId ON dbo.UserSites(SiteId) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Expenses_SiteId_ExpenseDate' AND object_id = OBJECT_ID('dbo.Expenses'))
    CREATE NONCLUSTERED INDEX IX_Expenses_SiteId_ExpenseDate ON dbo.Expenses(SiteId, ExpenseDate DESC) INCLUDE (Amount, Status, ExpenseTypeId) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ExpenseShares_ApartmentId_Status' AND object_id = OBJECT_ID('dbo.ExpenseShares'))
    CREATE NONCLUSTERED INDEX IX_ExpenseShares_ApartmentId_Status ON dbo.ExpenseShares(ApartmentId, Status) INCLUDE (Amount, PaidAmount, DueDate) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Payments_SiteId_PaymentDate' AND object_id = OBJECT_ID('dbo.Payments'))
    CREATE NONCLUSTERED INDEX IX_Payments_SiteId_PaymentDate ON dbo.Payments(SiteId, PaymentDate DESC) INCLUDE (Amount, ApartmentId, Method) WHERE IsDeleted = 0;
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Incomes_Site_Year_Month' AND object_id = OBJECT_ID('dbo.Incomes'))
    CREATE INDEX IX_Incomes_Site_Year_Month ON dbo.Incomes(SiteId, ApartmentId, [Year], [Month]);

-- ==================== VIEW ve PROSEDÜRLER ====================

GO
CREATE OR ALTER VIEW dbo.vw_SiteSummary AS
SELECT s.Id AS SiteId, s.Name AS SiteName,
    (SELECT COUNT(*) FROM dbo.Apartments a WHERE a.SiteId = s.Id AND a.IsDeleted = 0) AS ApartmentCount,
    (SELECT ISNULL(SUM(es.Amount - es.PaidAmount), 0) FROM dbo.ExpenseShares es
     JOIN dbo.Expenses e ON es.ExpenseId = e.Id AND e.SiteId = s.Id AND e.IsDeleted = 0
     WHERE es.IsDeleted = 0 AND es.Status IN (0, 1, 3)) AS TotalDebt,
    (SELECT ISNULL(SUM(p.Amount), 0) FROM dbo.Payments p WHERE p.SiteId = s.Id AND p.IsDeleted = 0
     AND p.PaymentDate >= DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)) AS CurrentMonthCollections
FROM dbo.Sites s WHERE s.IsDeleted = 0;
GO

IF (SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = 'SiteYonetim') = 0
    ALTER DATABASE SiteYonetim SET READ_COMMITTED_SNAPSHOT ON WITH NO_WAIT;

PRINT 'Site Yönetim veritabanı kurulumu tamamlandı.';
GO
