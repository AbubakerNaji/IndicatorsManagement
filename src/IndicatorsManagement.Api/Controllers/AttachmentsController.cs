using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Constants;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public class AttachmentsController : ControllerBase
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly IConfiguration _configuration;

    public AttachmentsController(IndicatorsDbContext db, IAuditLogService audit, IConfiguration configuration)
    {
        _db = db;
        _audit = audit;
        _configuration = configuration;
    }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private int UserEntityId => int.TryParse(User.FindFirstValue("EntityId"), out var eid) ? eid : 0;
    private string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    // Entity-scoped roles that must never see other entities' attachments (S5).
    private static readonly HashSet<string> EntityScopedRoles = new()
    {
        Roles.DataEntryUser, Roles.EntityAdmin, Roles.Reviewer
    };
    private bool IsEntityScoped => EntityScopedRoles.Contains(UserRole);

    // S3 — allowed content-type signatures. A file passes the sniff test if its first bytes
    // match one of these prefixes for a permitted extension. Prevents PDFs disguised as PNG etc.
    private static readonly Dictionary<string, byte[][]> MagicBytes = new()
    {
        [".pdf"]  = [[0x25, 0x50, 0x44, 0x46]],                                       // %PDF
        [".png"]  = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".jpg"]  = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        // Office 2007+ files are ZIP archives.
        [".xlsx"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]],
        [".docx"] = [[0x50, 0x4B, 0x03, 0x04], [0x50, 0x4B, 0x05, 0x06], [0x50, 0x4B, 0x07, 0x08]],
        // Legacy Office 97-2003 files are OLE compound documents (D0 CF 11 E0 A1 B1 1A E1).
        [".xls"]  = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        [".doc"]  = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]]
    };

    private static bool IsMagicBytesValid(string ext, byte[] header)
    {
        if (!MagicBytes.TryGetValue(ext, out var signatures)) return false;
        foreach (var sig in signatures)
        {
            if (header.Length < sig.Length) continue;
            bool matches = true;
            for (int i = 0; i < sig.Length; i++)
            {
                if (header[i] != sig[i]) { matches = false; break; }
            }
            if (matches) return true;
        }
        return false;
    }

    [HttpPost("indicator-entries/{entryId:int}/attachments")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.EntityAdmin},{Roles.DataEntryUser}")]
    public async Task<IActionResult> UploadAttachment(int entryId, IFormFile file)
    {
        var entry = await _db.IndicatorEntries.FindAsync(entryId);
        if (entry is null || entry.IsDeleted)
            return NotFound(ApiResponse.Fail("الإدخال غير موجود"));

        // S5 — entity-scoped users may only attach to their own entity's entries.
        if (IsEntityScoped && entry.EntityId != UserEntityId)
            return NotFound(ApiResponse.Fail("الإدخال غير موجود"));

        if (entry.WorkflowState != WorkflowState.Draft && entry.WorkflowState != WorkflowState.Returned_For_Modification)
            return BadRequest(ApiResponse.Fail("لا يمكن إضافة مرفقات في حالة الإدخال الحالية"));

        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse.Fail("لم يتم تحميل ملف"));

        // Validate file size (default 10MB)
        var maxSizeMb = 10;
        var configMaxSize = await _db.SystemConfigurations
            .Where(c => c.ConfigKey == ConfigKeys.FileUploadMaxSize)
            .Select(c => c.ConfigValue)
            .FirstOrDefaultAsync();
        if (configMaxSize != null) int.TryParse(configMaxSize, out maxSizeMb);

        if (file.Length > maxSizeMb * 1024 * 1024)
            return BadRequest(ApiResponse.Fail($"حجم الملف يتجاوز الحد الأقصى ({maxSizeMb} ميغابايت)"));

        // Validate file type by extension
        var allowedTypes = new[] { ".xlsx", ".xls", ".pdf", ".doc", ".docx", ".png", ".jpg", ".jpeg" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedTypes.Contains(ext))
            return BadRequest(ApiResponse.Fail("نوع الملف غير مدعوم"));

        // S3 — sniff magic bytes so a renamed executable can't pass extension-only checks.
        byte[] header = new byte[8];
        int read;
        using (var input = file.OpenReadStream())
        {
            read = await input.ReadAsync(header.AsMemory(0, header.Length));
        }
        if (read == 0 || !IsMagicBytesValid(ext, header))
            return BadRequest(ApiResponse.Fail("محتوى الملف لا يطابق نوعه المُصرَّح به"));

        // Save file — O1: honor UPLOADS_ROOT (mapped to a Docker volume in prod) so
        // uploads survive container replacement.
        var uploadsRoot = _configuration["UploadsRoot"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        var uploadsDir = Path.Combine(uploadsRoot, entryId.ToString());
        Directory.CreateDirectory(uploadsDir);
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var attachment = new Attachment
        {
            IndicatorEntryId = entryId,
            FileName = file.FileName,
            FilePath = filePath,
            FileType = ext,
            FileSize = file.Length,
            UploadedBy = UserId,
            UploadedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _db.Attachments.Add(attachment);
        await _db.SaveChangesAsync();

        await _audit.LogAsync(UserId, "Attachment", attachment.Id, "Upload_Attachment");

        return Ok(ApiResponse<AttachmentResponse>.Ok(new AttachmentResponse
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            FileType = attachment.FileType,
            FileSize = attachment.FileSize,
            Description = attachment.Description,
            UploadedAt = attachment.UploadedAt
        }, "تم رفع المرفق بنجاح"));
    }

    [HttpGet("attachments/{id:int}/download")]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var attachment = await _db.Attachments
            .Include(a => a.IndicatorEntry)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);
        if (attachment is null)
            return NotFound(ApiResponse.Fail("المرفق غير موجود"));

        // S5 — cross-entity download blocked with "not found" masking.
        if (IsEntityScoped && attachment.IndicatorEntry.EntityId != UserEntityId)
            return NotFound(ApiResponse.Fail("المرفق غير موجود"));

        if (!System.IO.File.Exists(attachment.FilePath))
            return NotFound(ApiResponse.Fail("ملف المرفق غير موجود على الخادم"));

        var bytes = await System.IO.File.ReadAllBytesAsync(attachment.FilePath);
        return File(bytes, "application/octet-stream", attachment.FileName);
    }

    [HttpDelete("attachments/{id:int}")]
    [Authorize(Roles = $"{Roles.SuperAdmin},{Roles.EntityAdmin},{Roles.DataEntryUser}")]
    public async Task<IActionResult> DeleteAttachment(int id)
    {
        var attachment = await _db.Attachments
            .Include(a => a.IndicatorEntry)
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (attachment is null)
            return NotFound(ApiResponse.Fail("المرفق غير موجود"));

        // S5 — same masking on delete.
        if (IsEntityScoped && attachment.IndicatorEntry.EntityId != UserEntityId)
            return NotFound(ApiResponse.Fail("المرفق غير موجود"));

        if (attachment.IndicatorEntry.WorkflowState != WorkflowState.Draft
            && attachment.IndicatorEntry.WorkflowState != WorkflowState.Returned_For_Modification)
            return BadRequest(ApiResponse.Fail("لا يمكن حذف المرفق في حالة الإدخال الحالية"));

        attachment.IsDeleted = true;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(UserId, "Attachment", id, "Delete_Attachment");

        return Ok(ApiResponse.Ok("تم حذف المرفق بنجاح"));
    }
}
