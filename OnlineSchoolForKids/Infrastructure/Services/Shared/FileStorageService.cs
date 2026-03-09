using Domain.Interfaces.Services.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services.Shared;

public class FileStorageService : IFileStorageService
{
    private readonly IConfiguration _configuration;
    private readonly string _uploadPath;
    private readonly string _baseUrl;
    private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    public FileStorageService(IConfiguration configuration)
    {
        _configuration = configuration;
        _uploadPath = _configuration["FileStorage:UploadPath"] ?? "wwwroot/uploads";
        _baseUrl = _configuration["FileStorage:BaseUrl"] ?? "/uploads";


        if (!Directory.Exists(_uploadPath))
        {
            Directory.CreateDirectory(_uploadPath);
        }
    }

    public async Task<string> UploadFileAsync(Stream file, string fileName, string folder)
    {
        try
        {
            // Create folder if it doesn't exist
            var folderPath = Path.Combine(_uploadPath, folder);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Generate unique file name
            var fileExtension = GetFileExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(folderPath, uniqueFileName);

            // Save file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return URL
            return $"{_baseUrl}/{folder}/{uniqueFileName}";
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to upload file: {ex.Message}", ex);
        }
    }

    public async Task DeleteFileAsync(string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            // Extract file path from URL
            var relativePath = fileUrl.Replace(_baseUrl, "").TrimStart('/');
            var filePath = Path.Combine(_uploadPath, relativePath);

            if (File.Exists(filePath))
            {
                await Task.Run(() => File.Delete(filePath));
            }
        }
        catch (Exception ex)
        {
            // Log error but don't throw - file deletion is not critical
            Console.WriteLine($"Failed to delete file: {ex.Message}");
        }
    }

    public string GetFileExtension(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant();
    }

    public bool IsImageFile(string fileName)
    {
        var extension = GetFileExtension(fileName);
        return _allowedImageExtensions.Contains(extension);
    }
}