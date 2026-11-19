namespace FrogHattery;

/// <summary>
/// A lovely pond where frogs live and socialize.
/// </summary>
public class FrogPond
{
    public string Name { get; set; } = "Sunny Meadow Pond";
    public double SurfaceArea { get; set; } = 500.0;
    public int LilypadCount { get; set; } = 25;
    public double WaterTemperature { get; set; } = 18.0;
    public Season CurrentSeason { get; set; } = Season.Summer;

    private readonly List<Frog> _residents = new();

    public int ResidentCount => _residents.Count;
    public double Capacity => SurfaceArea / 10;
    public bool IsFull => ResidentCount >= Capacity;

    public void AddResident( Frog frog )
    {
        if ( !IsFull )
        {
            _residents.Add( frog );
            Console.WriteLine( $"{frog.Name} moved into {Name}!" );
        }
        else
        {
            Console.WriteLine( $"Sorry, {Name} is at full capacity!" );
        }
    }

    public void RemoveResident( Frog frog )
    {
        if ( _residents.Remove( frog ) )
        {
            Console.WriteLine( $"{frog.Name} has left {Name}." );
        }
    }

    public IEnumerable<Frog> GetFrogsBySpecies( string species )
        => _residents.Where( f => f.Species == species );

    public IEnumerable<Frog> GetFrogsWithHats()
        => _residents.Where( f => f.IsWearingHat );

    public IEnumerable<Frog> GetFrogsWithoutHats()
        => _residents.Where( f => !f.IsWearingHat );

    public void UpdateWaterTemperature()
    {
        WaterTemperature = SeasonHelper.GetAverageTemperature( CurrentSeason );
        Console.WriteLine( $"Water temperature is now {WaterTemperature}°C" );
    }

    public void HostFashionShow()
    {
        Console.WriteLine( $"\n🎭 Welcome to the {Name} Fashion Show! 🎭\n" );
        foreach ( var frog in _residents.Where( f => f.IsWearingHat ) )
        {
            Console.WriteLine( $"  ✨ {frog.Name} struts in wearing a {frog.CurrentHat!.Name}!" );
        }
    }

    public void CallAllFrogs()
    {
        Console.WriteLine( $"\n🐸 {Name} Choir 🐸\n" );
        foreach ( var frog in _residents )
        {
            frog.Ribbit();
        }
    }

    public Frog? FindOldestFrog()
        => _residents.MaxBy( f => f.Age );

    public Frog? FindYoungestFrog()
        => _residents.MinBy( f => f.Age );

    public double AverageFrogAge()
        => _residents.Count > 0 ? _residents.Average( f => f.Age ) : 0;

    public void PrepareForSeason( Season season )
    {
        CurrentSeason = season;
        UpdateWaterTemperature();
        Console.WriteLine( $"{Name} is ready for {season}!" );
    }

    /// <summary>
    /// Static method to compare two frogs - ValidateFrog will be skipped here.
    /// </summary>
    public static int CompareFrogs( Frog left, Frog right )
        => left.Age.CompareTo( right.Age );
}
