using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Appointments
{
    public interface IAppointmentRepository
    {
        List<Appointment> GetAll();
        List<Appointment> GetByOwnerId(int ownerId);
        Appointment? GetById(int id);
        void Add(Appointment appointment);
        void Update(Appointment appointment);
        void Delete(Appointment appointment);
        void Save();
    }
}