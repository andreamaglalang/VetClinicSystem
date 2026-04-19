using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Appointments
{
    public interface IAppointmentService
    {
        List<Appointment> GetAll();
        List<Appointment> GetByUser(int userId);
        List<Appointment> Search(string? search);
        List<Appointment> SearchByUser(int userId, string? search);
        List<Appointment> Filter(string? search, int? statusId, DateOnly? appointmentDate);
        List<Appointment> FilterByUser(int userId, string? search, int? statusId, DateOnly? appointmentDate);
        Appointment? GetById(int id);
        void Add(Appointment appointment);
        void Update(Appointment appointment);
        void Delete(int id);
    }
}