namespace Modules.ProductModule;

internal class ProductService
{
    private readonly ILogger<ProductService> _logger;

    public ProductService(ILogger<ProductService> logger)
    {
        _logger = logger;
    }

    private static readonly List<Product> _products = new()
    {
        new Product { Id = 1, Name = "Laptop", Price = 999.99m, Category = "Electronics" },
        new Product { Id = 2, Name = "Mouse", Price = 29.99m, Category = "Electronics" },
        new Product { Id = 3, Name = "Keyboard", Price = 79.99m, Category = "Electronics" }
    };

    public List<Product> GetAll()
    {
        _logger.LogWarning("GET ALL PRODUCTS");
        return _products;
    }
}