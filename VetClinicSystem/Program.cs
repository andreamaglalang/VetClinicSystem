using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;
using VetClinicSystem.Repositories.Appointments;
using VetClinicSystem.Repositories.Clinics; 
using VetClinicSystem.Repositories.MedicalRecords;
using VetClinicSystem.Repositories.Notifications;
using VetClinicSystem.Repositories.Pets;
using VetClinicSystem.Repositories.Services;
using VetClinicSystem.Repositories.Users;
using VetClinicSystem.Repositories.Vaccinations;
using VetClinicSystem.Services.Appointments;
using VetClinicSystem.Services.Clinics;
using VetClinicSystem.Services.MedicalRecords;
using VetClinicSystem.Services.Notifications;
using VetClinicSystem.Services.Pets;
using VetClinicSystem.Services.Services;
using VetClinicSystem.Services.Users;
using VetClinicSystem.Services.Vaccinations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<VetClinicDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPetRepository, PetRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<IVaccinationRepository, VaccinationRepository>();
builder.Services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
builder.Services.AddScoped<IClinicRepository, ClinicRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IServiceManager, ServiceManager>();
builder.Services.AddScoped<IVaccinationService, VaccinationService>();
builder.Services.AddScoped<IMedicalRecordService, MedicalRecordService>();
builder.Services.AddScoped<IClinicService, ClinicService>();

var app = builder.Build();

EnsureApplicationSchema(app);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();   

app.Use(async (context, next) =>
{
    var notificationService = context.RequestServices.GetService<INotificationService>();
    var roleId = context.Session.GetInt32("RoleId");
    var userId = context.Session.GetInt32("UserId");

    if (notificationService != null && (roleId == 1 || roleId == 2))
    {
        context.Items["NotificationCount"] = notificationService.GetUnreadForStaff().Count;
    }
    else if (notificationService != null && roleId == 3 && userId.HasValue)
    {
        context.Items["NotificationCount"] = notificationService.GetUnreadForUser(userId.Value).Count;
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static void EnsureApplicationSchema(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();

    context.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('Users', 'MustChangePassword') IS NULL
BEGIN
    ALTER TABLE Users
    ADD MustChangePassword BIT NOT NULL DEFAULT 0;
END

IF COL_LENGTH('Users', 'LastPasswordChange') IS NULL
BEGIN
    ALTER TABLE Users
    ADD LastPasswordChange DATETIME NULL;
END

IF COL_LENGTH('Users', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE Users
    ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0;
END

IF COL_LENGTH('Users', 'DeletedAt') IS NULL
BEGIN
    ALTER TABLE Users
    ADD DeletedAt DATETIME NULL;
END

IF COL_LENGTH('Users', 'DeletedByUserId') IS NULL
BEGIN
    ALTER TABLE Users
    ADD DeletedByUserId INT NULL;
END

IF COL_LENGTH('Users', 'DeleteReason') IS NULL
BEGIN
    ALTER TABLE Users
    ADD DeleteReason NVARCHAR(255) NULL;
END

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
    ADD SurgeryLoadPoints INT NOT NULL CONSTRAINT DF_Appointments_SurgeryLoadPoints DEFAULT 0;
END

IF COL_LENGTH('Appointments', 'IsEmergency') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD IsEmergency BIT NOT NULL CONSTRAINT DF_Appointments_IsEmergency DEFAULT 0;
END

IF COL_LENGTH('Appointments', 'IsScheduleFinalized') IS NULL
BEGIN
    ALTER TABLE Appointments
    ADD IsScheduleFinalized BIT NOT NULL CONSTRAINT DF_Appointments_IsScheduleFinalized DEFAULT 0;
END

IF COL_LENGTH('Pets', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE Pets
    ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Pets_IsDeleted DEFAULT 0;
END

IF COL_LENGTH('Pets', 'DeletedAt') IS NULL
BEGIN
    ALTER TABLE Pets
    ADD DeletedAt DATETIME NULL;
END

IF COL_LENGTH('StaffNotifications', 'UserId') IS NULL
BEGIN
    ALTER TABLE StaffNotifications
    ADD UserId INT NULL;
END

IF COL_LENGTH('StaffNotifications', 'RecipientRole') IS NULL
BEGIN
    ALTER TABLE StaffNotifications
    ADD RecipientRole NVARCHAR(20) NOT NULL CONSTRAINT DF_StaffNotifications_RecipientRole DEFAULT 'Staff';
END

IF COL_LENGTH('StaffNotifications', 'IsArchived') IS NULL
BEGIN
    ALTER TABLE StaffNotifications
    ADD IsArchived BIT NOT NULL CONSTRAINT DF_StaffNotifications_IsArchived DEFAULT 0;
END

IF COL_LENGTH('StaffNotifications', 'ArchivedAt') IS NULL
BEGIN
    ALTER TABLE StaffNotifications
    ADD ArchivedAt DATETIME NULL;
END

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID('StaffNotifications')
      AND name = 'AppointmentId'
      AND is_nullable = 0)
BEGIN
    DECLARE @dropAppointmentFkSql NVARCHAR(MAX) = N'';
    SELECT @dropAppointmentFkSql = @dropAppointmentFkSql +
        N'ALTER TABLE StaffNotifications DROP CONSTRAINT [' + fk.name + N'];'
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('StaffNotifications')
      AND c.name = 'AppointmentId';

    IF (@dropAppointmentFkSql <> N'')
        EXEC sp_executesql @dropAppointmentFkSql;

    ALTER TABLE StaffNotifications
    ALTER COLUMN AppointmentId INT NULL;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('Users')
      AND c.name = 'DeletedByUserId')
BEGIN
    ALTER TABLE Users
    ADD CONSTRAINT FK_Users_Users_DeletedByUserId FOREIGN KEY (DeletedByUserId) REFERENCES Users(Id);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('StaffNotifications')
      AND c.name = 'AppointmentId')
BEGIN
    ALTER TABLE StaffNotifications
    ADD CONSTRAINT FK_StaffNotifications_Appointments_AppointmentId FOREIGN KEY (AppointmentId) REFERENCES Appointments(Id);
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
    WHERE fk.parent_object_id = OBJECT_ID('StaffNotifications')
      AND c.name = 'UserId')
BEGIN
    ALTER TABLE StaffNotifications
    ADD CONSTRAINT FK_StaffNotifications_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(Id);
END

IF COL_LENGTH('StaffNotifications', 'RecipientRole') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE StaffNotifications
        SET RecipientRole = ''Staff''
        WHERE RecipientRole IS NULL OR LTRIM(RTRIM(RecipientRole)) = '''';
    ');
END

IF COL_LENGTH('Appointments', 'PreferredAppointmentDate') IS NOT NULL
   AND COL_LENGTH('Appointments', 'PreferredAppointmentTime') IS NOT NULL
   AND COL_LENGTH('Appointments', 'SurgeryCategory') IS NOT NULL
   AND COL_LENGTH('Appointments', 'SurgeryLoadPoints') IS NOT NULL
   AND COL_LENGTH('Appointments', 'IsEmergency') IS NOT NULL
BEGIN
    EXEC(N'
        UPDATE a
        SET
            PreferredAppointmentDate = ISNULL(a.PreferredAppointmentDate, a.AppointmentDate),
            PreferredAppointmentTime = ISNULL(a.PreferredAppointmentTime, a.AppointmentTime),
            SurgeryCategory = CASE
                WHEN a.SurgeryCategory IS NULL OR LTRIM(RTRIM(a.SurgeryCategory)) = '''' THEN ''Moderate''
                ELSE a.SurgeryCategory
            END,
            SurgeryLoadPoints = CASE
                WHEN a.IsEmergency = 1 THEN 0
                WHEN a.SurgeryCategory = ''Minor'' THEN 1
                WHEN a.SurgeryCategory = ''Major'' THEN 3
                WHEN a.SurgeryCategory = ''Moderate'' THEN 2
                WHEN a.SurgeryLoadPoints IS NULL OR a.SurgeryLoadPoints = 0 THEN 2
                ELSE a.SurgeryLoadPoints
            END
        FROM Appointments a
        INNER JOIN Services s ON s.Id = a.ServiceId
        WHERE s.ServiceName LIKE ''%Surgery%''
    ');
END
");
}
