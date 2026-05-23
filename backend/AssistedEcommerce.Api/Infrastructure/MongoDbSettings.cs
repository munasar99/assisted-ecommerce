namespace AssistedEcommerce.Api.Infrastructure;

public class MongoDbSettings
{
    public const string SectionName = "MongoDb";
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "assisted_ecommerce";
}
