using System.Text.Json;
using BG.Application.Contracts.Services;
using BG.Application.Models.Intake;
using Microsoft.Extensions.Configuration;

namespace BG.Infrastructure.Storage;

internal sealed class LocalIntakeDocumentStore : IIntakeDocumentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _rootPath;

    public LocalIntakeDocumentStore(IConfiguration configuration)
    {
        _rootPath = configuration["Storage:DocumentsRoot"]
            ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "documents");
    }

    public async Task<StagedIntakeDocumentDto> StageAsync(
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var sanitizedOriginalFileName = SanitizeFileName(originalFileName);
        var token = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(sanitizedOriginalFileName);
        var stagingDirectory = GetStagingDirectory();
        Directory.CreateDirectory(stagingDirectory);

        var stagedFilePath = Path.Combine(stagingDirectory, $"{token}{extension}");
        var metadataPath = Path.Combine(stagingDirectory, $"{token}.json");

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using (var fileStream = File.Create(stagedFilePath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        var fileInfo = new FileInfo(stagedFilePath);
        var metadata = new StagedDocumentMetadata(
            sanitizedOriginalFileName,
            extension,
            fileInfo.Length);

        await File.WriteAllTextAsync(
            metadataPath,
            JsonSerializer.Serialize(metadata, JsonOptions),
            cancellationToken);

        return new StagedIntakeDocumentDto(token, sanitizedOriginalFileName, fileInfo.Length, stagedFilePath);
    }

    public async Task<PromotedIntakeDocumentDto> PromoteAsync(
        string stagedDocumentToken,
        string guaranteeNumber,
        CancellationToken cancellationToken = default)
    {
        var stagingDirectory = GetStagingDirectory();
        var metadataPath = Path.Combine(stagingDirectory, $"{stagedDocumentToken}.json");

        if (!File.Exists(metadataPath))
        {
            throw new FileNotFoundException("The staged document metadata was not found.", metadataPath);
        }

        var metadata = JsonSerializer.Deserialize<StagedDocumentMetadata>(
                           await File.ReadAllTextAsync(metadataPath, cancellationToken),
                           JsonOptions)
                       ?? throw new FileNotFoundException("The staged document metadata is invalid.", metadataPath);

        var stagedFilePath = Path.Combine(stagingDirectory, $"{stagedDocumentToken}{metadata.Extension}");

        if (!File.Exists(stagedFilePath))
        {
            throw new FileNotFoundException("The staged document file was not found.", stagedFilePath);
        }

        var sanitizedGuaranteeNumber = SanitizePathSegment(guaranteeNumber);
        var guaranteeDirectory = Path.Combine(_rootPath, "guarantees", sanitizedGuaranteeNumber);
        Directory.CreateDirectory(guaranteeDirectory);

        var finalFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{stagedDocumentToken[..6]}_{metadata.OriginalFileName}";
        var finalFilePath = Path.Combine(guaranteeDirectory, finalFileName);

        File.Move(stagedFilePath, finalFilePath);
        File.Delete(metadataPath);

        var relativePath = string.Join(
            '/',
            "guarantees",
            sanitizedGuaranteeNumber,
            finalFileName);

        return new PromotedIntakeDocumentDto(metadata.OriginalFileName, relativePath, metadata.FileSize);
    }

    private string GetStagingDirectory()
    {
        return Path.Combine(_rootPath, "intake", "staging");
    }

    private static string SanitizeFileName(string originalFileName)
    {
        var fileName = Path.GetFileName(string.IsNullOrWhiteSpace(originalFileName) ? "document.bin" : originalFileName.Trim());
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Select(character => invalidChars.Contains(character) ? '_' : character).ToArray());

        return string.IsNullOrWhiteSpace(sanitized) ? "document.bin" : sanitized;
    }

    private static string SanitizePathSegment(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Trim().Select(character => invalidChars.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }

    private sealed record StagedDocumentMetadata(
        string OriginalFileName,
        string Extension,
        long FileSize);
}
