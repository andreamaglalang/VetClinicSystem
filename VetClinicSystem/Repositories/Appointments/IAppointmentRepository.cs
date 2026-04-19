using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Appointments
{
    public interface IAppointmentRepository
    {
        List<Appointment> GetAll();
        List<Appointment> GetByOwnerId(int ownerId);
        List<Appointment> Search(string? search);
        List<Appointment> SearchByOwnerId(int ownerId, string? search);
        List<Appointment> Filter(string? search, int? statusId, DateOnly? appointmentDate);
        List<Appointment> FilterByOwnerId(int ownerId, string? search, int? statusId, DateOnly? appointmentDate);
        Appointment? GetById(int id);
        void Add(Appointment appointment);
        void Update(Appointment appointment);
        void Delete(Appointment appointment);
        void Save();
    }
}