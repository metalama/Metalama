namespace FrogHattery;

/// <summary>
/// The four seasons when frogs need different hats.
/// </summary>
public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

/// <summary>
/// Provides seasonal information for frog fashion decisions.
/// </summary>
public static class SeasonHelper
{
    public static bool IsWarm( Season season ) => season is Season.Spring or Season.Summer;
    public static bool IsCold( Season season ) => season is Season.Autumn or Season.Winter;
    public static bool IsRainy( Season season ) => season == Season.Spring;
    public static bool IsSunny( Season season ) => season == Season.Summer;
    public static bool IsWindy( Season season ) => season == Season.Autumn;
    public static bool IsSnowy( Season season ) => season == Season.Winter;

    public static Season Next( Season season ) => season switch
    {
        Season.Spring => Season.Summer,
        Season.Summer => Season.Autumn,
        Season.Autumn => Season.Winter,
        Season.Winter => Season.Spring,
        _ => Season.Spring
    };

    public static Season Previous( Season season ) => season switch
    {
        Season.Spring => Season.Winter,
        Season.Summer => Season.Spring,
        Season.Autumn => Season.Summer,
        Season.Winter => Season.Autumn,
        _ => Season.Spring
    };

    public static int GetAverageTemperature( Season season ) => season switch
    {
        Season.Spring => 15,
        Season.Summer => 28,
        Season.Autumn => 12,
        Season.Winter => 2,
        _ => 15
    };
}
