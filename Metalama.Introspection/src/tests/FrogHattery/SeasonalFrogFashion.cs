namespace FrogHattery;

/// <summary>
/// Provides seasonal fashion advice for frogs seeking the perfect hat.
/// </summary>
public class SeasonalFrogFashion
{
    public Season CurrentSeason { get; set; }

    public SeasonalFrogFashion( Season season )
    {
        CurrentSeason = season;
    }

    public string GetFashionAdvice( Frog frog )
    {
        return CurrentSeason switch
        {
            Season.Spring => $"{frog.Name} should try a colorful beret for the spring showers!",
            Season.Summer => $"{frog.Name} needs a wide-brimmed sun hat for pond lounging!",
            Season.Autumn => $"{frog.Name} would look dashing in a top hat for the falling leaves!",
            Season.Winter => $"{frog.Name} must get a cozy winter cap before hibernation!",
            _ => "Fashion is timeless!"
        };
    }

    public bool IsHatAppropriate( Hat hat )
        => hat.IsSuitableForWeather( CurrentSeason );

    public int CalculateFashionScore( Frog frog, Hat hat )
    {
        var baseScore = hat.FashionScore;

        // Seasonal bonus
        if ( hat.BestSeason == CurrentSeason )
            baseScore += 20;

        // Color coordination bonus
        if ( hat.Color.Equals( frog.FavoriteColor, StringComparison.OrdinalIgnoreCase ) )
            baseScore += 15;

        // Size fit bonus
        var sizeDiff = Math.Abs( hat.Size - frog.HeadCircumference );
        if ( sizeDiff < 0.5 )
            baseScore += 10;

        return baseScore;
    }

    public string GetSeasonalGreeting()
    {
        return CurrentSeason switch
        {
            Season.Spring => "Happy hopping! Spring has sprung!",
            Season.Summer => "Stay cool, froggy friends!",
            Season.Autumn => "Time to leap into fall fashion!",
            Season.Winter => "Bundle up, pond pals!",
            _ => "Welcome!"
        };
    }

    public IEnumerable<Hat> RankHatsForFrog( Frog frog, IEnumerable<Hat> hats )
        => hats.OrderByDescending( h => CalculateFashionScore( frog, h ) );

    public bool ShouldFrogWearHat( Season season )
        => season switch
        {
            Season.Summer => true, // Sun protection
            Season.Winter => true, // Warmth
            Season.Spring => true, // Rain protection
            Season.Autumn => true, // Style points
            _ => false
        };

    public string GetWeatherWarning()
    {
        return CurrentSeason switch
        {
            Season.Spring => "Watch out for spring showers - waterproof hats recommended!",
            Season.Summer => "UV levels high - ensure adequate brim width!",
            Season.Autumn => "Windy conditions - secure your hat with a chin strap!",
            Season.Winter => "Freezing temps ahead - prioritize warmth rating!",
            _ => "Check weather before hopping out!"
        };
    }

    public double GetRecommendedBrimWidth()
        => CurrentSeason == Season.Summer ? 5.0 : 2.0;

    public int GetRecommendedWarmthRating()
        => CurrentSeason switch
        {
            Season.Winter => 9,
            Season.Autumn => 6,
            Season.Spring => 4,
            Season.Summer => 1,
            _ => 5
        };
}
