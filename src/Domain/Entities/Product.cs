using Domain.Common;

namespace Domain.Entities;

public class Product : AuditableEntity
{
    public string ProductName { get; set; } = string.Empty;

    public ICollection<Item> Items { get; set; } = new List<Item>();
}
