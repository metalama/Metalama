namespace FrogHattery;

/// <summary>
/// Base class for all frogs who appreciate fine headwear.
/// </summary>
public abstract class Frog
{
    public string Name { get; set; } = "Anonymous Frog";
    public int Age { get; set; }
    public double HeadCircumference { get; set; }
    public string FavoriteColor { get; set; } = "Green";
    public Hat? CurrentHat { get; private set; }
    public Season FavoriteSeason { get; set; } = Season.Spring;

    public abstract string Species { get; }
    public abstract string Sound { get; }
    public abstract double JumpHeight { get; }

    public bool IsWearingHat => CurrentHat != null;
    public bool CanSwim { get; protected set; } = true;
    public bool IsNocturnal { get; protected set; } = false;

    public virtual void Ribbit() => Console.WriteLine( $"{Name} says: {Sound}!" );

    public virtual void Jump() => Console.WriteLine( $"{Name} jumps {JumpHeight}m high!" );

    public void PutOnHat( Hat hat )
    {
        if ( hat.Size >= HeadCircumference * 0.9 && hat.Size <= HeadCircumference * 1.2 )
        {
            CurrentHat = hat;
            Console.WriteLine( $"{Name} is now wearing a stylish {hat.Name}!" );
        }
        else
        {
            Console.WriteLine( $"The {hat.Name} doesn't fit {Name}'s head!" );
        }
    }

    public void TakeOffHat()
    {
        if ( CurrentHat != null )
        {
            Console.WriteLine( $"{Name} removes the {CurrentHat.Name}." );
            CurrentHat = null;
        }
    }

    public virtual bool LikesSeason( Season season ) => season == FavoriteSeason;

    public virtual void Hibernate() => Console.WriteLine( $"{Name} is hibernating..." );

    public virtual void WakeUp() => Console.WriteLine( $"{Name} wakes up refreshed!" );

    public int CalculateHatBudget() => Age * 10 + (int)(HeadCircumference * 5);

    public override string ToString() => $"{Name} the {Species}";
}
