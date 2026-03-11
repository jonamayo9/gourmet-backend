using GourmetApi.Data;
using GourmetApi.Enums;
using GourmetApi.Models.Tables;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GourmetApi.Controllers
{
    [ApiController]
    [Route("api/admin/{companySlug}/tables")]
    [Authorize]
    public class AdminTablesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminTablesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<List<TableListItemDto>>> GetTables(
            [FromRoute] string companySlug,
            [FromQuery] string? status = null)
        {
            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var companyId = company.Id;

            var tables = await _db.RestaurantTables
                .AsNoTracking()
                .Where(x => x.CompanyId == companyId && x.Enabled)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Number)
                .ToListAsync();

            var activeStatuses = new[]
            {
                TableSessionStatus.Open,
                TableSessionStatus.BillRequested,
                TableSessionStatus.Paid
            };

            var activeSessions = await _db.TableSessions
                .AsNoTracking()
                .Where(x =>
                    x.CompanyId == companyId &&
                    activeStatuses.Contains(x.Status))
                .OrderByDescending(x => x.OpenedAt)
                .ToListAsync();

            var latestSessionByTable = activeSessions
                .GroupBy(x => x.RestaurantTableId)
                .ToDictionary(
                    g => g.Key,
                    g => g.First());

            var result = tables.Select(t =>
            {
                latestSessionByTable.TryGetValue(t.Id, out var currentSession);

                var sessionStatus = currentSession?.Status;

                var mappedStatus =
                    currentSession == null ? "Free" :
                    sessionStatus == TableSessionStatus.Open ? "Open" :
                    sessionStatus == TableSessionStatus.BillRequested ? "BillRequested" :
                    sessionStatus == TableSessionStatus.Paid ? "Paid" :
                    "Free";

                return new TableListItemDto
                {
                    TableId = t.Id,
                    Number = t.Number,
                    Name = string.IsNullOrWhiteSpace(t.Name) ? $"Mesa {t.Number}" : t.Name,
                    Capacity = t.Capacity,
                    Status = mappedStatus,
                    SessionId = currentSession?.Id,
                    TotalGuests = currentSession?.TotalGuests,
                    Adults = currentSession?.Adults,
                    Children = currentSession?.Children,
                    CurrentTotal = currentSession?.Total ?? 0,
                    OpenedAt = currentSession?.OpenedAt
                };
            }).ToList();

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                var normalized = status.Trim().ToLower();

                result = normalized switch
                {
                    "free" => result.Where(x => x.Status == "Free").ToList(),
                    "open" => result.Where(x => x.Status == "Open").ToList(),
                    "bill-requested" => result.Where(x => x.Status == "BillRequested").ToList(),
                    "paid" => result.Where(x => x.Status == "Paid").ToList(),
                    _ => result
                };
            }

            return Ok(result);
        }

[HttpPost("{tableId:int}/open")]
        public async Task<ActionResult> OpenTable(
    [FromRoute] string companySlug,
    [FromRoute] int tableId,
    [FromBody] OpenTableRequestDto request)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            if (!company.TablesEnabled)
                return BadRequest("La gestión de mesas no está habilitada para esta empresa.");

            var table = await _db.RestaurantTables
                .FirstOrDefaultAsync(x => x.Id == tableId && x.CompanyId == company.Id && x.Enabled);

            if (table == null)
                return NotFound("Mesa no encontrada.");

            if (request.TotalGuests <= 0)
                return BadRequest("La cantidad de personas debe ser mayor a 0.");

            if (request.TotalGuests > table.Capacity)
                return BadRequest($"La mesa permite hasta {table.Capacity} persona(s).");

            if (company.EnableAdultsChildrenSplit && company.RequireAdultsChildrenSplit)
            {
                if (request.Adults == null || request.Children == null)
                    return BadRequest("Debés informar adultos y menores.");

                if ((request.Adults.Value + request.Children.Value) != request.TotalGuests)
                    return BadRequest("La suma de adultos y menores debe coincidir con el total de personas.");
            }

            var hasActiveSession = await _db.TableSessions.AnyAsync(x =>
                x.CompanyId == company.Id &&
                x.RestaurantTableId == tableId &&
                x.Status != TableSessionStatus.Closed);

            if (hasActiveSession)
                return BadRequest("La mesa ya se encuentra ocupada.");

            var session = new TableSession
            {
                CompanyId = company.Id,
                RestaurantTableId = table.Id,
                Status = TableSessionStatus.Open,
                TotalGuests = request.TotalGuests,
                Adults = company.EnableAdultsChildrenSplit ? request.Adults : null,
                Children = company.EnableAdultsChildrenSplit ? request.Children : null,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                Total = 0,
                OpenedAt = DateTime.UtcNow
            };

            _db.TableSessions.Add(session);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Mesa abierta correctamente.",
                sessionId = session.Id,
                tableId = table.Id,
                status = session.Status.ToString()
            });
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<TableHistoryDto>>> GetHistory(
    [FromRoute] string companySlug,
    [FromQuery] DateTime? date = null)
        {
            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var selectedDate = (date ?? DateTime.UtcNow).Date;
            var from = DateTime.SpecifyKind(selectedDate, DateTimeKind.Utc);
            var to = from.AddDays(1);

            var sessions = await _db.TableSessions
                .AsNoTracking()
                .Include(x => x.RestaurantTable)
                .Include(x => x.Items)
                .Where(x =>
                    x.CompanyId == company.Id &&
                    x.OpenedAt >= from &&
                    x.OpenedAt < to)
                .OrderByDescending(x => x.OpenedAt)
                .ToListAsync();

            var result = sessions.Select(session => new TableHistoryDto
            {
                SessionId = session.Id,
                TableId = session.RestaurantTableId,
                TableNumber = session.RestaurantTable.Number,
                TableName = string.IsNullOrWhiteSpace(session.RestaurantTable.Name)
                    ? $"Mesa {session.RestaurantTable.Number}"
                    : session.RestaurantTable.Name,

                Status = session.Status,

                TotalGuests = session.TotalGuests,
                Adults = session.Adults,
                Children = session.Children,

                Total = session.Total,

                PaymentMethod = session.PaymentMethod,
                PaymentStatus = session.PaymentStatus,

                OpenedAt = session.OpenedAt,
                ClosedAt = session.ClosedAt,

                Items = session.Items
                    .OrderBy(i => i.Id)
                    .Select(i => new TableHistoryItemDto
                    {
                        Name = i.Name,
                        Qty = i.Qty,
                        UnitPrice = i.UnitPrice,
                        LineTotal = i.LineTotal,
                        Note = i.Note,
                        IsManual = i.IsManual,
                        IsInternalProduct = i.IsInternalProduct
                    })
                    .ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpGet("config")]
        public async Task<ActionResult<TableConfigDto>> GetTableConfig([FromRoute] string companySlug)
        {
            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var dto = new TableConfigDto
            {
                TablesEnabled = company.TablesEnabled,
                EnableGuestCount = company.EnableGuestCount,
                EnableAdultsChildrenSplit = company.EnableAdultsChildrenSplit,
                RequireAdultsChildrenSplit = company.RequireAdultsChildrenSplit
            };

            return Ok(dto);
        }

        [HttpPut("config")]
        public async Task<ActionResult> UpdateTableConfig(
    [FromRoute] string companySlug,
    [FromBody] TableConfigDto request)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            if (!request.EnableAdultsChildrenSplit && request.RequireAdultsChildrenSplit)
                return BadRequest("No podés requerir mayores/menores si esa opción está deshabilitada.");

            company.TablesEnabled = request.TablesEnabled;
            company.EnableGuestCount = request.EnableGuestCount;
            company.EnableAdultsChildrenSplit = request.EnableAdultsChildrenSplit;
            company.RequireAdultsChildrenSplit = request.RequireAdultsChildrenSplit;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Configuración de mesas actualizada correctamente."
            });
        }

        [HttpPost("generate")]
        public async Task<ActionResult> GenerateTables(
    [FromRoute] string companySlug,
    [FromBody] GenerateTablesDto request)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            if (request.Count <= 0)
                return BadRequest("La cantidad de mesas debe ser mayor a 0.");

            if (request.DefaultCapacity <= 0)
                return BadRequest("La capacidad debe ser mayor a 0.");

            var existingTables = await _db.RestaurantTables
                .Where(x => x.CompanyId == company.Id)
                .ToListAsync();

            if (existingTables.Any())
                return BadRequest("La empresa ya tiene mesas creadas.");

            var tables = new List<RestaurantTable>();

            for (int i = 1; i <= request.Count; i++)
            {
                tables.Add(new RestaurantTable
                {
                    CompanyId = company.Id,
                    Number = i,
                    Name = $"Mesa {i}",
                    Capacity = request.DefaultCapacity,
                    Enabled = true,
                    Order = i,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _db.RestaurantTables.AddRange(tables);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Mesas generadas correctamente.",
                count = tables.Count
            });
        }

        [HttpGet("setup")]
        public async Task<ActionResult> GetTablesSetup(
            [FromRoute] string companySlug,
            [FromQuery] bool includeDeleted = false)
        {
            var company = await _db.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var openTableIds = await _db.TableSessions
                .AsNoTracking()
                .Where(x =>
                    x.CompanyId == company.Id &&
                    x.Status != GourmetApi.Enums.TableSessionStatus.Closed)
                .Select(x => x.RestaurantTableId)
                .Distinct()
                .ToListAsync();

            var query = _db.RestaurantTables
                .AsNoTracking()
                .Where(x => x.CompanyId == company.Id);

            if (!includeDeleted)
                query = query.Where(x => !x.IsDeleted);

            var tables = await query
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Number)
                .Select(x => new
                {
                    id = x.Id,
                    number = x.Number,
                    name = x.Name,
                    capacity = x.Capacity,
                    enabled = x.Enabled,
                    order = x.Order,
                    isDeleted = x.IsDeleted,
                    deletedAt = x.DeletedAt,
                    createdAt = x.CreatedAt
                })
                .ToListAsync();

            var result = tables.Select(x => new
            {
                x.id,
                x.number,
                x.name,
                x.capacity,
                x.enabled,
                x.order,
                x.isDeleted,
                x.deletedAt,
                x.createdAt,
                hasOpenSession = openTableIds.Contains(x.id)
            });

            return Ok(result);
        }

        [HttpPost]
        public async Task<ActionResult> CreateTable(
    [FromRoute] string companySlug,
    [FromBody] UpsertRestaurantTableDto request)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            if (request.Number <= 0)
                return BadRequest("El número de mesa debe ser mayor a 0.");

            if (request.Capacity <= 0)
                return BadRequest("La capacidad debe ser mayor a 0.");

            var exists = await _db.RestaurantTables.AnyAsync(x =>
                x.CompanyId == company.Id &&
                x.Number == request.Number &&
                !x.IsDeleted);

            if (exists)
                return BadRequest("Ya existe una mesa con ese número.");

            var table = new RestaurantTable
            {
                CompanyId = company.Id,
                Number = request.Number,
                Name = string.IsNullOrWhiteSpace(request.Name) ? $"Mesa {request.Number}" : request.Name.Trim(),
                Capacity = request.Capacity,
                Enabled = request.Enabled,
                Order = request.Order > 0 ? request.Order : request.Number,
                CreatedAt = DateTime.UtcNow
            };

            _db.RestaurantTables.Add(table);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Mesa creada correctamente.",
                tableId = table.Id
            });
        }

        [HttpPut("{tableId:int}")]
        public async Task<ActionResult> UpdateTable(
    [FromRoute] string companySlug,
    [FromRoute] int tableId,
    [FromBody] UpsertRestaurantTableDto request)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var table = await _db.RestaurantTables
                .FirstOrDefaultAsync(x => x.Id == tableId && x.CompanyId == company.Id);

            if (table == null)
                return NotFound("Mesa no encontrada.");

            if (request.Number <= 0)
                return BadRequest("El número de mesa debe ser mayor a 0.");

            if (request.Capacity <= 0)
                return BadRequest("La capacidad debe ser mayor a 0.");

            var duplicated = await _db.RestaurantTables.AnyAsync(x =>
            x.CompanyId == company.Id &&
            x.Id != tableId &&
            x.Number == request.Number &&
            !x.IsDeleted);

            if (duplicated)
                return BadRequest("Ya existe otra mesa con ese número.");

            table.Number = request.Number;
            table.Name = string.IsNullOrWhiteSpace(request.Name) ? $"Mesa {request.Number}" : request.Name.Trim();
            table.Capacity = request.Capacity;
            table.Enabled = request.Enabled;
            table.Order = request.Order > 0 ? request.Order : request.Number;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Mesa actualizada correctamente."
            });
        }

        [HttpDelete("{tableId:int}")]
        public async Task<ActionResult> DeleteTable(
    [FromRoute] string companySlug,
    [FromRoute] int tableId)
        {
            var company = await _db.Companies
                .FirstOrDefaultAsync(x => x.Slug == companySlug);

            if (company == null)
                return NotFound("Empresa no encontrada.");

            var table = await _db.RestaurantTables
                .FirstOrDefaultAsync(x =>
                    x.Id == tableId &&
                    x.CompanyId == company.Id);

            if (table == null)
                return NotFound("Mesa no encontrada.");

            if (table.IsDeleted)
                return BadRequest("La mesa ya fue eliminada.");

            var hasOpenSession = await _db.TableSessions
                .AnyAsync(x =>
                    x.CompanyId == company.Id &&
                    x.RestaurantTableId == table.Id &&
                    x.Status != GourmetApi.Enums.TableSessionStatus.Closed);

            if (hasOpenSession)
                return BadRequest("No podés eliminar una mesa que está abierta.");

            table.IsDeleted = true;
            table.DeletedAt = DateTime.UtcNow;
            table.Enabled = false;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Mesa eliminada correctamente."
            });
        }
    }
}