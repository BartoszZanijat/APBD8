using HospitalApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HospitalApi.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController(HospitalContext context) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatientResponseDto>>> GetPatients([FromQuery] string? search)
    {
        var query = context.Patients
            .AsNoTracking()
            .Include(p => p.Admissions)
                .ThenInclude(a => a.Ward)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.BedType)
            .Include(p => p.BedAssignments)
                .ThenInclude(ba => ba.Bed)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Ward)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.FirstName, pattern) ||
                EF.Functions.Like(p.LastName, pattern));
        }

        var patients = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Select(p => new PatientResponseDto(
                p.Pesel,
                p.FirstName,
                p.LastName,
                p.Age,
                p.Sex ? "Male" : "Female",
                p.Admissions
                    .OrderBy(a => a.AdmissionDate)
                    .Select(a => new AdmissionDto(
                        a.Id,
                        a.AdmissionDate,
                        a.DischargeDate,
                        new WardDto(a.Ward.Id, a.Ward.Name, a.Ward.Description)))
                    .ToList(),
                p.BedAssignments
                    .OrderBy(ba => ba.From)
                    .Select(ba => new BedAssignmentDto(
                        ba.Id,
                        ba.From,
                        ba.To,
                        new BedDto(
                            ba.Bed.Id,
                            new BedTypeDto(
                                ba.Bed.BedType.Id,
                                ba.Bed.BedType.Name,
                                ba.Bed.BedType.Description),
                            new RoomDto(
                                ba.Bed.Room.Id,
                                ba.Bed.Room.HasTv,
                                new WardDto(
                                    ba.Bed.Room.Ward.Id,
                                    ba.Bed.Room.Ward.Name,
                                    ba.Bed.Room.Ward.Description)))))
                    .ToList()))
            .ToListAsync();

        return Ok(patients);
    }

    [HttpPost("{pesel}/bedassignments")]
    public async Task<ActionResult<CreateBedAssignmentResponseDto>> AssignBed(
        [FromRoute] string pesel,
        [FromBody] CreateBedAssignmentRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.BedType))
        {
            return BadRequest(new ErrorDto("Pole 'bedType' jest wymagane."));
        }

        if (string.IsNullOrWhiteSpace(request.Ward))
        {
            return BadRequest(new ErrorDto("Pole 'ward' jest wymagane."));
        }

        if (request.To.HasValue && request.To <= request.From)
        {
            return BadRequest(new ErrorDto("Pole 'to' musi być późniejsze niż 'from'."));
        }

        var patientExists = await context.Patients.AnyAsync(p => p.Pesel == pesel);
        if (!patientExists)
        {
            return NotFound(new ErrorDto($"Pacjent o PESEL '{pesel}' nie istnieje."));
        }

        var ward = await context.Wards
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Name == request.Ward);

        if (ward is null)
        {
            return NotFound(new ErrorDto($"Oddział '{request.Ward}' nie istnieje."));
        }

        var bedType = await context.BedTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(bt => bt.Name == request.BedType);

        if (bedType is null)
        {
            return NotFound(new ErrorDto($"Typ łóżka '{request.BedType}' nie istnieje."));
        }

        var from = request.From;
        var to = request.To;

        var freeBed = await context.Beds
            .Where(b => b.BedTypeId == bedType.Id && b.Room.WardId == ward.Id)
            .Where(b => !b.BedAssignments.Any(ba =>
                (to ?? DateTime.MaxValue) > ba.From &&
                from < (ba.To ?? DateTime.MaxValue)))
            .OrderBy(b => b.Id)
            .FirstOrDefaultAsync();

        if (freeBed is null)
        {
            return NotFound(new ErrorDto("Brak wolnego łóżka o podanym typie i oddziale w wybranym zakresie czasu."));
        }

        var assignment = new BedAssignment
        {
            PatientPesel = pesel,
            BedId = freeBed.Id,
            From = from,
            To = to
        };

        context.BedAssignments.Add(assignment);
        await context.SaveChangesAsync();

        return Ok(new CreateBedAssignmentResponseDto(
            assignment.Id,
            assignment.PatientPesel,
            assignment.BedId,
            assignment.From,
            assignment.To));
    }
}

public record PatientResponseDto(
    string Pesel,
    string FirstName,
    string LastName,
    int Age,
    string Sex,
    IReadOnlyCollection<AdmissionDto> Admissions,
    IReadOnlyCollection<BedAssignmentDto> BedAssignments);

public record AdmissionDto(int Id, DateTime AdmissionDate, DateTime? DischargeDate, WardDto Ward);

public record WardDto(int Id, string Name, string Description);

public record BedAssignmentDto(int Id, DateTime From, DateTime? To, BedDto Bed);

public record BedDto(int Id, BedTypeDto BedType, RoomDto Room);

public record BedTypeDto(int Id, string Name, string Description);

public record RoomDto(string Id, bool HasTv, WardDto Ward);

public record CreateBedAssignmentRequestDto(DateTime From, DateTime? To, string BedType, string Ward);

public record CreateBedAssignmentResponseDto(int Id, string PatientPesel, int BedId, DateTime From, DateTime? To);

public record ErrorDto(string Message);
