namespace FrogHattery;

/// <summary>
/// An elegant top hat for the most distinguished frogs.
/// </summary>
public class TopHat : Hat
{
    public override Season BestSeason => Season.Autumn;
    public override string Style => "Formal";

    public double Height { get; set; } = 5.0;
    public bool HasRibbon { get; set; } = true;
    public string RibbonColor { get; set; } = "Black";
    public bool IsCollapsible { get; set; } = false;

    public TopHat()
    {
        Name = "Top Hat";
        Material = "Silk";
        Color = "Black";
        Price = 89.99m;
        FashionScore = 95;
    }

    public void Tip()
    {
        Console.WriteLine( "Tips the top hat gracefully. 'Good day!'" );
    }

    public void Collapse()
    {
        if ( IsCollapsible )
        {
            Console.WriteLine( "The top hat collapses flat for easy storage!" );
        }
        else
        {
            Console.WriteLine( "This top hat cannot be collapsed." );
        }
    }

    public void PullOutRabbit()
    {
        Console.WriteLine( "A tiny toy rabbit appears from the hat! Magic!" );
    }

    public void AdjustRibbon()
    {
        Console.WriteLine( $"Straightens the {RibbonColor} ribbon perfectly." );
    }

    public override string GetDescription()
        => $"A magnificent {Height}cm tall {Color} {Name} with {RibbonColor} ribbon";

    public double CalculateTotalHeight( double frogHeight ) => frogHeight + Height;
}
