namespace FrogHattery;

/// <summary>
/// A wide-brimmed sun hat for summer pond lounging.
/// </summary>
public class SunHat : Hat
{
    public override Season BestSeason => Season.Summer;
    public override string Style => "Casual";

    public double BrimWidth { get; set; } = 4.0;
    public int UVProtectionLevel { get; set; } = 50;
    public bool HasChinStrap { get; set; } = true;
    public bool IsFloatable { get; set; } = true;
    public string FlowerDecoration { get; set; } = "None";

    public SunHat()
    {
        Name = "Sun Hat";
        Material = "Straw";
        Color = "Natural";
        Price = 24.99m;
        FashionScore = 70;
        IsWaterproof = false;
    }

    public void AddFlower( string flowerType )
    {
        FlowerDecoration = flowerType;
        FashionScore += 10;
        Console.WriteLine( $"A lovely {flowerType} now adorns the sun hat!" );
    }

    public void RemoveFlower()
    {
        FlowerDecoration = "None";
        Console.WriteLine( "The flower decoration has been removed." );
    }

    public void FlapInWind()
    {
        if ( !HasChinStrap )
        {
            Console.WriteLine( "The sun hat flies away in the breeze!" );
        }
        else
        {
            Console.WriteLine( "The chin strap keeps the hat secure." );
        }
    }

    public void FloatOnWater()
    {
        if ( IsFloatable )
        {
            Console.WriteLine( "The sun hat doubles as a tiny boat!" );
        }
    }

    public double CalculateShadeArea() => Math.PI * BrimWidth * BrimWidth / 4;

    public override bool IsSuitableForWeather( Season season )
        => season == Season.Summer || (season == Season.Spring && BrimWidth > 3);

    public override string GetDescription()
    {
        var desc = $"A {BrimWidth}cm brim {Color} {Name}";
        return FlowerDecoration != "None" ? $"{desc} with {FlowerDecoration}" : desc;
    }
}
