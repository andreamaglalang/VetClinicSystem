IF COL_LENGTH('StaffNotifications', 'IsArchived') IS NULL
BEGIN
    ALTER TABLE StaffNotifications
    ADD IsArchived BIT NOT NULL
        CONSTRAINT DF_StaffNotifications_IsArchived DEFAULT 0;
END
GO

IF COL_LENGTH('StaffNotifications', 'ArchivedAt') IS NULL
BEGIN
    ALTER TABLE StaffNotifications
    ADD ArchivedAt DATETIME NULL;
END
GO

UPDATE StaffNotifications
SET IsArchived = 0
WHERE IsArchived IS NULL;
GO
