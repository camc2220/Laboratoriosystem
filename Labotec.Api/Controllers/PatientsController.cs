using Labotec.Api.Data;
using Labotec.Api.DTOs;
using Labotec.Api.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Labotec.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly AppDbContext _db;
    public PatientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<PagedResult<PatientReadDto>>> Get([FromQuery] string? q, [FromQuery] int page=1, [FromQuery] int pageSize=20, [FromQuery] string? sortBy=null, [FromQuery] string sortDir="asc")
    {
        var query = _db.Patients.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.FullName.Contains(q) || p.DocumentId.Contains(q));
        var total = await query.CountAsync();
        var pageQ = query.ApplyOrdering(sortBy, sortDir).ApplyPaging(page, pageSize);
        var data = await pageQ.Select(p => new PatientReadDto(p.Id, p.FullName, p.DocumentId, p.BirthDate, p.Email, p.Phone)).ToListAsync();
        return Ok(new PagedResult<PatientReadDto>(data, page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PatientReadDto>> GetOne(Guid id)
    {
        var p = await _db.Patients.FindAsync(id);
        if (p is null) return NotFound();
        return new PatientReadDto(p.Id, p.FullName, p.DocumentId, p.BirthDate, p.Email, p.Phone);
    }

    [HttpPost]
    public async Task<ActionResult<PatientReadDto>> Create([FromBody] PatientCreateDto dto)
    {
        var entity = new Domain.Patient { FullName = dto.FullName, DocumentId = dto.DocumentId, BirthDate = dto.BirthDate, Email = dto.Email, Phone = dto.Phone };
        _db.Patients.Add(entity); await _db.SaveChangesAsync();
        var result = new PatientReadDto(entity.Id, entity.FullName, entity.DocumentId, entity.BirthDate, entity.Email, entity.Phone);
        return CreatedAtAction(nameof(GetOne), new { id = entity.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PatientUpdateDto dto)
    {
        var p = await _db.Patients.FindAsync(id);
        if (p is null) return NotFound();
        p.FullName = dto.FullName; p.BirthDate = dto.BirthDate; p.Email = dto.Email; p.Phone = dto.Phone;
        await _db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await _db.Patients.FindAsync(id);
        if (p is null) return NotFound();
        _db.Remove(p); await _db.SaveChangesAsync(); return NoContent();
    }
}
