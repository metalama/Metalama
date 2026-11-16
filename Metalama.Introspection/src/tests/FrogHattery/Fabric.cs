using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace FrogHattery;

/// <summary>
/// Project fabric that applies aspects to methods across the project.
/// Some will be applied, some will be skipped due to eligibility.
/// </summary>
public class FrogHatteryFabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        // Apply Log to all methods in FrogHatShop - all will succeed.
        amender.SelectMany( p => p.Types.Where( t => t.Name == "FrogHatShop" ) )
            .SelectMany( t => t.Methods )
            .AddAspectIfEligible<LogAttribute>();

        // Apply Log to all methods in Frog - abstract methods will be skipped.
        amender.SelectMany( p => p.Types.Where( t => t.Name == "Frog" ) )
            .SelectMany( t => t.Methods )
            .AddAspectIfEligible<LogAttribute>();

        // Apply ValidateFrog to methods with Frog or Hat parameters (including derived types).
        // Static methods will be skipped.
        amender.SelectMany( p => p.Types )
            .SelectMany( t => t.Methods )
            .Where( m => m.Parameters.Any( p => p.Type.IsConvertibleTo( typeof(Frog) ) || p.Type.IsConvertibleTo( typeof(Hat) ) ) )
            .AddAspectIfEligible<ValidateFrogAttribute>();
    }
}
