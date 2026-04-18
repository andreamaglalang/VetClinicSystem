using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Appointments
{
    public interface IAppointmentRepository
    {
        List<Appointment> GetAll();
        Appointment? GetById(int id);
        void Add(Appointment appointment);
        void Update(Appointment appointment);
        void Delete(Appointment appointment);
        void Save();
    }
}