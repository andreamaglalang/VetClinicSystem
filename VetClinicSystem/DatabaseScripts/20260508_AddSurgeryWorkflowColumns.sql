IF COL_LENGTH('Appointments', 'PreferredAppointmentDate') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD PreferredAppointmentDate DATE NULL;
END

IF COL_LENGTH('Appointments', 'PreferredAppointmentTime') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD PreferredAppointmentTime TIME NULL;
END

IF COL_LENGTH('Appointments', 'SurgeryCategory') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD SurgeryCategory NVARCHAR(20) NULL;
END

IF COL_LENGTH('Appointments', 'SurgeryLoadPoints') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD SurgeryLoadPoints INT NOT NULL
        CONSTRAINT DF_Appointments_SurgeryLoadPoints DEFAULT 0;
END

IF COL_LENGTH('Appointments', 'IsEmergency') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD IsEmergency BIT NOT NULL
        CONSTRAINT DF_Appointments_IsEmergency DEFAULT 0;
END

IF COL_LENGTH('Appointments', 'IsScheduleFinalized') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD IsScheduleFinalized BIT NOT NULL
        CONSTRAINT DF_Appointments_IsScheduleFinalized DEFAULT 0;
END

UPDATE a
SET
    PreferredAppointmentDate = ISNULL(a.PreferredAppointmentDate, a.AppointmentDate),
    PreferredAppointmentTime = ISNULL(a.PreferredAppointmentTime, a.AppointmentTime),
    SurgeryCategory = CASE
        WHEN a.SurgeryCategory IS NULL OR LTRIM(RTRIM(a.SurgeryCategory)) = '' THEN 'Moderate'
        ELSE a.SurgeryCategory
    END,
    SurgeryLoadPoints = CASE
        WHEN a.IsEmergency = 1 THEN 0
        WHEN a.SurgeryCategory = 'Minor' THEN 1
        WHEN a.SurgeryCategory = 'Major' THEN 3
        WHEN a.SurgeryCategory = 'Moderate' THEN 2
        WHEN a.SurgeryLoadPoints IS NULL OR a.SurgeryLoadPoints = 0 THEN 2
        ELSE a.SurgeryLoadPoints
    END
FROM Appointments a
INNER JOIN Services s ON s.Id = a.ServiceId
WHERE s.ServiceName LIKE '%Surgery%';
