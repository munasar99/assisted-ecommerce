namespace AssistedEcommerce.Api.Infrastructure;

public static class FormFileHelper
{
    public static IFormFile FromBytes(byte[] bytes, IFormFile source)
    {
        var stream = new MemoryStream(bytes, writable: false);
        return new FormFile(stream, 0, bytes.Length, source.Name, source.FileName)
        {
            Headers = source.Headers,
            ContentType = source.ContentType
        };
    }
}
