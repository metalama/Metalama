namespace FrogHattery;

/// <summary>
/// A cozy winter cap to keep frogs warm during hibernation season.
/// </summary>
public class WinterCap : Hat
{
    public override Season BestSeason => Season.Winter;
    public override string Style => "Cozy";

    public bool HasEarFlaps { get; set; } = true;
    public bool HasPomPom { get; set; } = true;
    public string PomPomColor { get; set; } = "White";
    public int WarmthRating { get; set; } = 9;
    public bool IsLinedWithFleece { get; set; } = true;

    public WinterCap()
    {
        Name = "Winter Cap";
        Material = "Knit Wool";
        Color = "Green";
        Price = 29.99m;
        FashionScore = 75;
        IsWaterproof = false;
    }

    public void FlipDownEarFlaps()
    {
        if ( HasEarFlaps )
        {
            Console.WriteLine( "Ear flaps down - maximum warmth activated!" );
            WarmthRating += 2;
        }
    }

    public void FlipUpEarFlaps()
    {
        if ( HasEarFlaps )
        {
            Console.WriteLine( "Ear flaps up - stylish mode engaged!" );
            WarmthRating -= 2;
        }
    }

    public void BouncePomPom()
    {
        if ( HasPomPom )
        {
            Console.WriteLine( $"The {PomPomColor} pom-pom bounces adorably!" );
        }
    }

    public void SnuggleFit()
    {
        Console.WriteLine( "Pulls the cap down snugly. So cozy!" );
    }

    public bool CanProtectFromSnow() => IsWaterproof || IsLinedWithFleece;

    public double CalculateWarmthEfficiency()
        => WarmthRating * (IsLinedWithFleece ? 1.5 : 1.0) * (HasEarFlaps ? 1.3 : 1.0);

    public override bool IsSuitableForWeather( Season season )
        => season == Season.Winter || (season == Season.Autumn && WarmthRating < 7);

    public override string GetDescription()
    {
        var extras = new List<string>();
        if ( HasEarFlaps ) extras.Add( "ear flaps" );
        if ( HasPomPom ) extras.Add( $"{PomPomColor} pom-pom" );
        var extrasStr = extras.Count > 0 ? $" with {string.Join( " and ", extras )}" : "";
        return $"A warm {Color} {Name}{extrasStr}";
    }
}
