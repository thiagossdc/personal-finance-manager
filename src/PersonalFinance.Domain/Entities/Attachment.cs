using PersonalFinance.Domain.Common;

namespace PersonalFinance.Domain.Entities;

/// <summary>Anexo de um arquivo (ex.: foto de um comprovante) a uma transação.</summary>
public class Attachment : Entity
{
    public Guid UserId { get; private set; }
    public Guid TransactionId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StorageKey { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    private Attachment() { }

    public static Result<Attachment> Create(string fileName, byte[] bytes, string contentType, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Error.Validation("File name is required.");
        }

        if (bytes.Length == 0)
        {
            return Error.Validation("Attachment cannot be empty.");
        }

        return Result<Attachment>.Success(new Attachment
        {
            FileName = fileName,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            SizeBytes = bytes.Length,
            StorageKey = storageKey,
            CreatedAtUtc = DateTime.UtcNow
        });
    }
}
