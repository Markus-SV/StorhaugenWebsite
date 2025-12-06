using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenEats.API.DTOs;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/storage")]
[Authorize]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly ICurrentUserService _currentUserService;

    public StorageController(IStorageService storageService, ICurrentUserService currentUserService)
    {
        _storageService = storageService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Upload an image to Supabase Storage
    /// </summary>
    [HttpPost("upload")]
    public async Task<ActionResult<UploadImageResultDto>> UploadImage([FromBody] UploadImageDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Base64Data))
            return BadRequest(new { message = "Image data is required" });

        if (string.IsNullOrWhiteSpace(dto.FileName))
            return BadRequest(new { message = "File name is required" });

        // Validate file extension
        var extension = Path.GetExtension(dto.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        if (!allowedExtensions.Contains(extension))
            return BadRequest(new { message = $"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}" });

        try
        {
            // Convert base64 to bytes
            var imageBytes = Convert.FromBase64String(dto.Base64Data);

            // Check file size (5MB max)
            const int maxSizeBytes = 5 * 1024 * 1024; // 5MB
            if (imageBytes.Length > maxSizeBytes)
                return BadRequest(new { message = "Image size must be less than 5MB" });

            // Generate unique filename
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var uniqueFileName = $"{userId}_{Guid.NewGuid()}{extension}";

            // Upload to storage
            var url = await _storageService.UploadImageAsync(imageBytes, uniqueFileName, dto.Bucket);

            return Ok(new UploadImageResultDto
            {
                Url = url,
                FileName = uniqueFileName
            });
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid base64 image data" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to upload image", error = ex.Message });
        }
    }

    /// <summary>
    /// Delete an image from Supabase Storage
    /// </summary>
    [HttpDelete("{fileName}")]
    public async Task<IActionResult> DeleteImage(string fileName, [FromQuery] string bucket = "recipe-images")
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest(new { message = "File name is required" });

        try
        {
            // Extract user ID from filename (format: {userId}_{guid}.ext)
            var parts = fileName.Split('_');
            if (parts.Length < 2 || !int.TryParse(parts[0], out var fileUserId))
                return BadRequest(new { message = "Invalid file name format" });

            // Verify user owns the file
            var currentUserId = await _currentUserService.GetOrCreateUserIdAsync();
            if (fileUserId != currentUserId)
                return Forbid();

            await _storageService.DeleteImageAsync(fileName, bucket);

            return Ok(new { message = "Image deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to delete image", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a signed URL for uploading directly to storage (for large files)
    /// </summary>
    [HttpPost("upload-url")]
    public async Task<IActionResult> GetUploadUrl([FromBody] GetUploadUrlDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var uniqueFileName = $"{userId}_{Guid.NewGuid()}{Path.GetExtension(dto.FileName)}";

        // In a full implementation, you'd generate a signed URL from Supabase Storage
        // For now, return the file name that should be used
        return Ok(new
        {
            fileName = uniqueFileName,
            message = "Use POST /api/storage/upload endpoint for file upload"
        });
    }
}

public record GetUploadUrlDto(string FileName);
