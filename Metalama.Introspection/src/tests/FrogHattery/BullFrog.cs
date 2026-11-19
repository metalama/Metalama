namespace FrogHattery;

/// <summary>
/// A large, powerful bullfrog with an impressive voice.
/// </summary>
public class BullFrog : Frog
{
    public override string Species => "Bull Frog";
    public override string Sound => "JUG-O-RUM";
    public override double JumpHeight => 1.3;

    public double Weight { get; set; } = 0.5;
    public double TerritorySize { get; set; } = 10.0;
    public bool IsAlphaFrog { get; set; } = false;
    public int FliesCaughtToday { get; private set; }
    public int CricketsCaughtToday { get; private set; }

    public BullFrog()
    {
        HeadCircumference = 8.0;
        CanSwim = true;
    }

    public void CatchFly()
    {
        FliesCaughtToday++;
        Console.WriteLine( $"{Name} catches a fly with lightning speed! ({FliesCaughtToday} today)" );
    }

    public void CatchCricket()
    {
        CricketsCaughtToday++;
        Console.WriteLine( $"{Name} snatches a cricket! ({CricketsCaughtToday} today)" );
    }

    public void DefendTerritory()
    {
        Console.WriteLine( $"{Name} bellows loudly: {Sound}! This pond is MINE!" );
    }

    public void ChallengeFrog( Frog otherFrog )
    {
        if ( otherFrog is BullFrog otherBull && otherBull.Weight > Weight )
        {
            Console.WriteLine( $"{Name} wisely backs away from {otherFrog.Name}." );
        }
        else
        {
            Console.WriteLine( $"{Name} challenges {otherFrog.Name} with a mighty croak!" );
        }
    }

    public void Dive() => Console.WriteLine( $"{Name} dives deep into the pond!" );

    public void SunOnLilypad() => Console.WriteLine( $"{Name} lounges on a lilypad, looking majestic." );

    public override void Ribbit()
    {
        Console.WriteLine( $"{Name} BELLOWS: {Sound}! {Sound}!" );
    }

    public int TotalInsectsCaught() => FliesCaughtToday + CricketsCaughtToday;

    public double CalculateDailyCalories() => FliesCaughtToday * 2.5 + CricketsCaughtToday * 4.0;
}
