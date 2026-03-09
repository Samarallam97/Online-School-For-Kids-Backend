namespace Domain.Interfaces.Services.Shared;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream file, string fileName, string folder);
    Task DeleteFileAsync(string fileUrl);
    string GetFileExtension(string fileName);
    bool IsImageFile(string fileName);
}