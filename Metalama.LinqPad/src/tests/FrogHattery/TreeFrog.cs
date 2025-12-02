namespace FrogHattery;

/// <summary>
/// A small, agile tree frog with excellent climbing abilities.
/// </summary>
public class TreeFrog : Frog
{
    public override string Species => "Tree Frog";
    public override string Sound => "Peep peep";
    public override double JumpHeight => 0.5;

    public double ClimbingSpeed { get; set; } = 2.5;
    public bool HasStickyToes { get; set; } = true;
    public string PreferredTreeType { get; set; } = "Oak";
    public int BranchesClimbedToday { get; private set; }

    public TreeFrog()
    {
        HeadCircumference = 2.0;
        IsNocturnal = true;
    }

    public void ClimbTree( string treeName )
    {
        if ( HasStickyToes )
        {
            Console.WriteLine( $"{Name} swiftly climbs the {treeName}!" );
            BranchesClimbedToday++;
        }
        else
        {
            Console.WriteLine( $"{Name} struggles to climb without sticky toes." );
        }
    }

    public void DescendTree() => Console.WriteLine( $"{Name} gracefully descends from the tree." );

    public void CamouflageOnLeaf() => Console.WriteLine( $"{Name} blends perfectly with the foliage!" );

    public override bool LikesSeason( Season season ) => season == Season.Summer;

    public void SingNightSong() => Console.WriteLine( $"{Name} joins the evening chorus: {Sound}!" );

    public double CalculateDailyClimbDistance() => BranchesClimbedToday * ClimbingSpeed * 0.3;
}
