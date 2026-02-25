using Microsoft.EntityFrameworkCore;
using MEDICSYS.Api.Data;
using MEDICSYS.Api.Models.Academico;

namespace MEDICSYS.Api.Services;

public class AcademicScopeService
{
    private readonly AcademicDbContext _db;

    public AcademicScopeService(AcademicDbContext db)
    {
        _db = db;
    }

    public async Task<HashSet<Guid>> GetSupervisedStudentIdsAsync(
        Guid professorId,
        bool includeFallback = true,
        CancellationToken cancellationToken = default)
    {
        var assignedIds = await _db.AcademicSupervisionAssignments
            .AsNoTracking()
            .Where(a => a.IsActive && a.ProfessorId == professorId)
            .Select(a => a.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (!includeFallback)
        {
            return assignedIds.ToHashSet();
        }

        var fallbackFromAppointments = await _db.AcademicAppointments
            .AsNoTracking()
            .Where(a => a.ProfessorId == professorId)
            .Select(a => a.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var fallbackFromReviews = await _db.AcademicClinicalHistories
            .AsNoTracking()
            .Where(h => h.ReviewedByProfessorId == professorId)
            .Select(h => h.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return assignedIds
            .Concat(fallbackFromAppointments)
            .Concat(fallbackFromReviews)
            .ToHashSet();
    }

    public async Task<bool> ProfessorSupervisesStudentAsync(
        Guid professorId,
        Guid studentId,
        bool includeFallback = true,
        CancellationToken cancellationToken = default)
    {
        var supervisedIds = await GetSupervisedStudentIdsAsync(professorId, includeFallback, cancellationToken);
        return supervisedIds.Contains(studentId);
    }

    public async Task<HashSet<Guid>> GetAccessiblePatientIdsAsync(
        Guid professorId,
        CancellationToken cancellationToken = default)
    {
        var createdPatientIds = await _db.AcademicPatients
            .AsNoTracking()
            .Where(p => p.CreatedByProfessorId == professorId)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var assignedPatientIds = await _db.AcademicSupervisionAssignments
            .AsNoTracking()
            .Where(a => a.IsActive && a.ProfessorId == professorId && a.PatientId.HasValue)
            .Select(a => a.PatientId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        return createdPatientIds
            .Concat(assignedPatientIds)
            .ToHashSet();
    }

    public async Task<bool> ProfessorCanAccessPatientAsync(
        Guid professorId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        if (await _db.AcademicPatients.AsNoTracking().AnyAsync(
                p => p.Id == patientId && p.CreatedByProfessorId == professorId,
                cancellationToken))
        {
            return true;
        }

        return await _db.AcademicSupervisionAssignments.AsNoTracking().AnyAsync(
            a => a.IsActive
                 && a.ProfessorId == professorId
                 && a.PatientId == patientId,
            cancellationToken);
    }
}
