namespace FrogHattery;

/// <summary>
/// Base class for all hats suitable for frogs.
/// </summary>
public abstract class Hat
{
    public string Name { get; set; } = "Generic Hat";
    public double Size { get; set; }
    public string Color { get; set; } = "Brown";
    public string Material { get; set; } = "Felt";
    public decimal Price { get; set; }
    public bool IsWaterproof { get; set; }
    public int FashionScore { get; set; }

    public abstract Season BestSeason { get; }
    public abstract string Style { get; }

    public virtual bool IsSuitableForWeather( Season season ) => season == BestSeason;

    public virtual string GetDescription() => $"A {Color} {Name} made of {Material}";

    public void ApplyWaterproofing()
    {
        IsWaterproof = true;
        Console.WriteLine( $"The {Name} is now waterproof!" );
    }

    public void AddDecoration( string decoration )
    {
        Console.WriteLine( $"Added {decoration} to the {Name}!" );
        FashionScore += 5;
    }

    public void Polish() => Console.WriteLine( $"The {Name} now shines brilliantly!" );

    public void Resize( double newSize )
    {
        Size = newSize;
        Console.WriteLine( $"The {Name} has been resized to {newSize}cm." );
    }

    public decimal CalculateDiscountedPrice( int discountPercent )
        => Price * (100 - discountPercent) / 100;

    public override string ToString() => $"{Name} ({Style}) - ${Price}";
}
