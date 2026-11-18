namespace FrogHattery;

/// <summary>
/// The finest frog millinery establishment this side of the lily pond.
/// </summary>
public class FrogHatShop
{
    public string Name { get; } = "Le Chapeau de la Grenouille";
    public string Location { get; set; } = "Main Pond, Left Side";
    public bool IsOpen { get; private set; }
    public Season CurrentSeason { get; set; } = Season.Spring;

    private readonly List<Hat> _inventory = new();
    private readonly List<Frog> _customers = new();
    private readonly Dictionary<Frog, List<Hat>> _purchases = new();

    public int InventoryCount => _inventory.Count;
    public int CustomerCount => _customers.Count;
    public decimal TotalRevenue { get; private set; }

    public void Open()
    {
        IsOpen = true;
        Console.WriteLine( $"🎩 {Name} is now OPEN! Welcome, distinguished frogs!" );
    }

    public void Close()
    {
        IsOpen = false;
        Console.WriteLine( $"{Name} is now closed. See you tomorrow!" );
    }

    public void AddToInventory( Hat hat )
    {
        _inventory.Add( hat );
        Console.WriteLine( $"Added {hat.Name} to inventory. (Total: {InventoryCount})" );
    }

    public void RemoveFromInventory( Hat hat )
    {
        _inventory.Remove( hat );
        Console.WriteLine( $"Removed {hat.Name} from inventory." );
    }

    public void RegisterCustomer( Frog frog )
    {
        if ( !_customers.Contains( frog ) )
        {
            _customers.Add( frog );
            _purchases[frog] = new List<Hat>();
            Console.WriteLine( $"Welcome to {Name}, {frog.Name}!" );
        }
    }

    public void SellHat( Frog customer, Hat hat )
    {
        if ( !IsOpen )
        {
            Console.WriteLine( "Sorry, the shop is closed!" );
            return;
        }

        if ( !_inventory.Contains( hat ) )
        {
            Console.WriteLine( $"Sorry, {hat.Name} is out of stock." );
            return;
        }

        _inventory.Remove( hat );
        _purchases[customer].Add( hat );
        TotalRevenue += hat.Price;
        Console.WriteLine( $"{customer.Name} purchased {hat.Name} for ${hat.Price}!" );
    }

    public IEnumerable<Hat> GetSeasonalRecommendations()
        => _inventory.Where( h => h.BestSeason == CurrentSeason );

    public IEnumerable<Hat> GetHatsInBudget( decimal budget )
        => _inventory.Where( h => h.Price <= budget );

    public IEnumerable<Hat> FindHatsBySize( double minSize, double maxSize )
        => _inventory.Where( h => h.Size >= minSize && h.Size <= maxSize );

    public Hat? FindMostFashionable()
        => _inventory.MaxBy( h => h.FashionScore );

    public Hat? FindCheapestHat()
        => _inventory.MinBy( h => h.Price );

    public void DisplayInventory()
    {
        Console.WriteLine( $"\n=== {Name} Inventory ===" );
        foreach ( var hat in _inventory.OrderBy( h => h.Price ) )
        {
            var seasonal = hat.BestSeason == CurrentSeason ? "⭐ SEASONAL" : "";
            Console.WriteLine( $"  - {hat.GetDescription()} - ${hat.Price} {seasonal}" );
        }
    }

    public void AnnounceSeasonalSale()
    {
        Console.WriteLine( $"🎉 {CurrentSeason} SALE! All {CurrentSeason} hats 20% off!" );
    }

    public decimal GetTotalInventoryValue()
        => _inventory.Sum( h => h.Price );

    public Dictionary<string, int> GetInventoryByStyle()
        => _inventory.GroupBy( h => h.Style )
            .ToDictionary( g => g.Key, g => g.Count() );
}
