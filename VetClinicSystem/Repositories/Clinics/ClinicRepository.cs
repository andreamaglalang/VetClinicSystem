using System.Data;
using Microsoft.EntityFrameworkCore;
using VetClinicSystem.Models;

namespace VetClinicSystem.Repositories.Clinics
{
    public class ClinicRepository : IClinicRepository
    {
        private readonly VetClinicDbContext _context;

        public ClinicRepository(VetClinicDbContext context)
        {
            _context = context;
        }

        public ClinicInfo? GetClinicInfo()
        {
            using var command = _context.Database.GetDbConnection().CreateCommand();
            command.CommandText = @"
SELECT TOP 1
    Id,
    ClinicName,
    Address,
    ContactNumber,
    Email,
    OperatingHours,
    FacebookPage,
    AboutText,
    Mission,
    Vision,
    LastUpdated
FROM ClinicInfo
ORDER BY Id";
            command.CommandType = CommandType.Text;

            if (command.Connection?.State != ConnectionState.Open)
            {
                command.Connection?.Open();
            }

            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new ClinicInfo
            {
                Id = GetInt32(reader, "Id"),
                ClinicName = GetString(reader, "ClinicName") ?? string.Empty,
                Address = GetString(reader, "Address"),
                ContactNumber = GetString(reader, "ContactNumber"),
                Email = GetString(reader, "Email"),
                OperatingHours = GetString(reader, "OperatingHours"),
                FacebookPage = GetString(reader, "FacebookPage"),
                AboutText = GetString(reader, "AboutText"),
                Mission = GetString(reader, "Mission"),
                Vision = GetString(reader, "Vision"),
                LastUpdated = GetNullableDateTime(reader, "LastUpdated")
            };
        }

        public void Add(ClinicInfo clinicInfo)
        {
            _context.ClinicInfos.Add(clinicInfo);
        }

        public void Update(ClinicInfo clinicInfo)
        {
            _context.ClinicInfos.Update(clinicInfo);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        private static string? GetString(IDataRecord record, string columnName)
        {
            var ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? null : record.GetString(ordinal);
        }

        private static int GetInt32(IDataRecord record, string columnName)
        {
            var ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? 0 : record.GetInt32(ordinal);
        }

        private static DateTime? GetNullableDateTime(IDataRecord record, string columnName)
        {
            var ordinal = record.GetOrdinal(columnName);
            return record.IsDBNull(ordinal) ? null : record.GetDateTime(ordinal);
        }
    }
}
