namespace FrogHattery;

/// <summary>
/// A beautiful but toxic poison dart frog from the rainforest.
/// </summary>
public class PoisonDartFrog : Frog
{
    public override string Species => "Poison Dart Frog";
    public override string Sound => "Chirp chirp";
    public override double JumpHeight => 0.3;

    public string SkinColor { get; set; } = "Brilliant Blue";
    public string PatternType { get; set; } = "Spots";
    public double ToxicityLevel { get; set; } = 8.5;
    public bool IsFromCaptiveBreeding { get; set; } = true;
    public string NativeRegion { get; set; } = "Amazon Rainforest";

    public PoisonDartFrog()
    {
        HeadCircumference = 1.5;
        FavoriteColor = "Blue";
        FavoriteSeason = Season.Summer;
    }

    public void DisplayWarningColors()
    {
        Console.WriteLine( $"{Name} shows off its {SkinColor} skin with {PatternType}!" );
        Console.WriteLine( "Predators beware!" );
    }

    public void SecreteToxin()
    {
        if ( !IsFromCaptiveBreeding )
        {
            Console.WriteLine( $"{Name} secretes a powerful toxin! (Level: {ToxicityLevel})" );
        }
        else
        {
            Console.WriteLine( $"{Name} is non-toxic due to captive diet." );
        }
    }

    public void HuntAnts() => Console.WriteLine( $"{Name} stalks tiny ants in the leaf litter." );

    public void HuntTermites() => Console.WriteLine( $"{Name} finds a tasty termite!" );

    public void HideUnderLeaf() => Console.WriteLine( $"{Name} tucks under a moist leaf." );

    public void CallForMate()
    {
        Console.WriteLine( $"{Name} begins an elaborate courtship call: {Sound}!" );
    }

    public void CarryTadpoles() => Console.WriteLine( $"{Name} carries tadpoles on its back to water!" );

    public override bool LikesSeason( Season season ) => SeasonHelper.IsWarm( season );

    public double GetDisplayBrightness() => ToxicityLevel * 10;

    public bool IsSafeToHandle() => IsFromCaptiveBreeding && ToxicityLevel < 2.0;
}
