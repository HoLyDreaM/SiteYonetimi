-- =============================================
-- Migration: Apartments Unique Constraint - Blok Desteği
-- Sorun: A Blok'ta 1-16 numaralı daireler varken B Blok'ta aynı numaralar eklenemiyordu
-- Çözüm: Unique constraint (SiteId, ApartmentNumber) -> (SiteId, BlockOrBuildingName, ApartmentNumber)
-- Böylece A Blok 1, B Blok 1 ayrı kaydedilebilir
-- =============================================

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Apartments_Site_Number' AND object_id = OBJECT_ID('dbo.Apartments'))
    ALTER TABLE dbo.Apartments DROP CONSTRAINT UQ_Apartments_Site_Number;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Apartments_Site_Block_Number' AND object_id = OBJECT_ID('dbo.Apartments'))
    ALTER TABLE dbo.Apartments ADD CONSTRAINT UQ_Apartments_Site_Block_Number UNIQUE (SiteId, BlockOrBuildingName, ApartmentNumber);

PRINT 'Apartments unique constraint migration tamamlandı.';
GO
