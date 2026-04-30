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

    if (notificationService != null &&
        (context.Session.GetInt32("RoleId") == 1 || context.Session.GetInt32("RoleId") == 2))
    {
        context.Items["NotificationCount"] = notificationService.GetUnread().Count;
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();