namespace KMyBot.KMy.Models;

public class Account
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Currency { get; set; } = null!;
    public bool IsActive { get; set; }
    public IEnumerable<Account> Children { get; set; } = [];
}