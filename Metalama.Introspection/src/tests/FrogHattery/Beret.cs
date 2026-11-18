namespace FrogHattery;

/// <summary>
/// A stylish French beret for the artistically inclined frog.
/// </summary>
public class Beret : Hat
{
    public override Season BestSeason => Season.Spring;
    public override string Style => "Artistic";

    public bool HasStem { get; set; } = true;
    public string Tilt { get; set; } = "Left";
    public bool IsVintage { get; set; } = false;

    public Beret()
    {
        Name = "Beret";
        Material = "Wool";
        Color = "Red";
        Price = 34.99m;
        FashionScore = 80;
    }

    public void TiltLeft()
    {
        Tilt = "Left";
        Console.WriteLine( "The beret is now tilted rakishly to the left." );
    }

    public void TiltRight()
    {
        Tilt = "Right";
        Console.WriteLine( "The beret is now tilted stylishly to the right." );
    }

    public void StraightenBeret()
    {
        Tilt = "Center";
        Console.WriteLine( "The beret sits properly centered." );
    }

    public void PaintWhileWearing()
    {
        Console.WriteLine( "Feels inspired to create art! *ribbits in French*" );
    }

    public void RecitePoetry()
    {
        Console.WriteLine( "Clears throat... 'To hop or not to hop, that is the question.'" );
    }

    public override bool IsSuitableForWeather( Season season )
        => season == Season.Spring || season == Season.Autumn;

    public override string GetDescription()
        => $"A {(IsVintage ? "vintage " : "")}{Color} {Name} worn tilted {Tilt}";

    public int GetArtisticBonus() => IsVintage ? 15 : 10;
}
