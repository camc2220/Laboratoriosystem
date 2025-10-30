using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Labotec.Api.Storage;

public class AzureBlobService
{
    private readonly BlobServiceClient _svc;
    private readonly string _container;

    public AzureBlobService(IConfiguration cfg)
    {
        var conn = cfg.GetValue<string>("AzureBlob:ConnectionString")
            ?? throw new InvalidOperationException("AzureBlob:ConnectionString missing");
        _container = cfg.GetValue<string>("AzureBlob:Container") ?? "labresults";
        _svc = new BlobServiceClient(conn);
    }

    public async Task<string> UploadAsync(string fileName, Stream content, string contentType)
    {
        var container = _svc.GetBlobContainerClient(_container);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(fileName);
        await blob.UploadAsync(content, overwrite: true);
        await blob.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType });
        return blob.Uri.ToString();
    }

    public string GetSasLink(string fileName, TimeSpan ttl)
    {
        var container = _svc.GetBlobContainerClient(_container);
        var blob = container.GetBlobClient(fileName);
        if (!blob.CanGenerateSasUri) return blob.Uri.ToString();

        var sas = new BlobSasBuilder
        {
            BlobContainerName = container.Name,
            BlobName = fileName,
            Resource = "b",
            ExpiresOn = DateTimeOffset.UtcNow.Add(ttl)
        };
        sas.SetPermissions(BlobSasPermissions.Read);
        var uri = blob.GenerateSasUri(sas);
        return uri.ToString();
    }
}
