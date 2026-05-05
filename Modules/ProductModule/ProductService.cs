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

    public Product? FindById(int productId) => _products.Find(p => p.Id == productId);
    public bool DeleteById(int productId)
    {
        var product = FindById(productId);
        if (product is null) return false;

        _products.Remove(product);

        return true;
    }

    public bool Update(int productId, Product prodcutForUpdate)
    {
        var product = FindById(productId);
        if (product is null) return false;

        product.Name = prodcutForUpdate.Name;
        product.Price = prodcutForUpdate.Price;

        return true;
    }

    public bool Create(Product prodcutForUpdate)
    {
        prodcutForUpdate.Id = _products.OrderByDescending(x => x.Id).First().Id + 1;
        _products.Add(prodcutForUpdate);
        return true;
    }
}