using VetClinicSystem.Models;

namespace VetClinicSystem.Services.Appointments
{
    public interface IAppointmentService
    {
        List<Appointment> GetAll();
        List<Appointment> GetByUser(int userId);
        Appointment? GetById(int id);
        void Add(Appointment appointment);
        void Update(Appointment appointment);
        void Delete(int id);
    }
}