using Labotec.Api.Data;
using Labotec.Api.DTOs;
using Labotec.Api.Common;
using Labotec.Api.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Labotec.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ResultsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<LabResultReadDto>>> Get([FromQuery] Guid? patientId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? test, [FromQuery] int page=1, [FromQuery] int pageSize=20, [FromQuery] string? sortBy=null, [FromQuery] string sortDir="asc")
    {
        var q = _db.LabResults.AsNoTracking().Include(r => r.Patient).AsQueryable();
        if (patientId.HasValue) q = q.Where(r => r.PatientId == patientId.Value);
        if (from.HasValue) q = q.Where(r => r.ReleasedAt >= from.Value);
        if (to.HasValue) q = q.Where(r => r.ReleasedAt <= to.Value);
        if (!string.IsNullOrWhiteSpace(test)) q = q.Where(r => r.TestName.Contains(test));
        var total = await q.CountAsync();
        var pageQ = q.ApplyOrdering(sortBy, sortDir).ApplyPaging(page, pageSize);
        var data = await pageQ.Select(r => new LabResultReadDto(r.Id, r.PatientId, r.Patient.FullName, r.TestName, r.ResultValue, r.Unit, r.ReleasedAt, r.PdfUrl)).ToListAsync();
        return Ok(new PagedResult<LabResultReadDto>(data, page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LabResultReadDto>> GetOne(Guid id)
    {
        var r = await _db.LabResults.Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();
        return new LabResultReadDto(r.Id, r.PatientId, r.Patient.FullName, r.TestName, r.ResultValue, r.Unit, r.ReleasedAt, r.PdfUrl);
    }

    [HttpPost]
    public async Task<ActionResult<LabResultReadDto>> Create([FromBody] LabResultCreateDto dto)
    {
        var patient = await _db.Patients.FindAsync(dto.PatientId);
        if (patient is null) return BadRequest("Paciente no existe");
        var entity = new Domain.LabResult { PatientId = dto.PatientId, TestName = dto.TestName, ResultValue = dto.ResultValue, Unit = dto.Unit, ReleasedAt = dto.ReleasedAt ?? DateTime.UtcNow, PdfUrl = dto.PdfUrl };
        _db.LabResults.Add(entity); await _db.SaveChangesAsync();
        var result = new LabResultReadDto(entity.Id, patient.Id, patient.FullName, entity.TestName, entity.ResultValue, entity.Unit, entity.ReleasedAt, entity.PdfUrl);
        return CreatedAtAction(nameof(GetOne), new { id = entity.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] LabResultUpdateDto dto)
    {
        var r = await _db.LabResults.FindAsync(id);
        if (r is null) return NotFound();
        r.TestName = dto.TestName; r.ResultValue = dto.ResultValue; r.Unit = dto.Unit; r.ReleasedAt = dto.ReleasedAt; r.PdfUrl = dto.PdfUrl;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var r = await _db.LabResults.FindAsync(id);
        if (r is null) return NotFound();
        _db.Remove(r); await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpPost("{id:guid}/pdf")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> UploadPdf(Guid id, IFormFile file, [FromServices] AzureBlobService storage)
    {
        var r = await _db.LabResults.FindAsync(id);
        if (r is null) return NotFound();
        if (file is null || file.Length == 0) return BadRequest("Archivo vacío");
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".pdf") return BadRequest("Solo se permite PDF");
        var blobName = $"results/{id}.pdf";
        using var s = file.OpenReadStream();
        var url = await storage.UploadAsync(blobName, s, "application/pdf");
        r.PdfUrl = url; await _db.SaveChangesAsync();
        return Ok(new { url });
    }

    [HttpGet("{id:guid}/pdf-url")]
    public async Task<IActionResult> GetPdfSas(Guid id, [FromServices] AzureBlobService storage, [FromQuery] int minutes=30)
    {
        var r = await _db.LabResults.FindAsync(id);
        if (r is null || string.IsNullOrWhiteSpace(r.PdfUrl)) return NotFound();
        var fname = $"results/{id}.pdf";
        var sas = storage.GetSasLink(fname, TimeSpan.FromMinutes(minutes));
        return Ok(new { url = sas });
    }
}
