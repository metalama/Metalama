using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;

namespace FrogHattery;

/// <summary>
/// Aspect that logs method entry and exit for frog shop operations.
/// </summary>
public class LogAttribute : OverrideMethodAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        base.BuildEligibility( builder );

        // Skip abstract methods (will create skipped instances).
        builder.MustNotBeAbstract();
    }

    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( $"[LOG] Entering {meta.Target.Method.Name}" );
        try
        {
            var result = meta.Proceed();
            Console.WriteLine( $"[LOG] Exiting {meta.Target.Method.Name}" );
            return result;
        }
        catch ( Exception ex )
        {
            Console.WriteLine( $"[LOG] Error in {meta.Target.Method.Name}: {ex.Message}" );
            throw;
        }
    }
}

/// <summary>
/// Aspect that validates frog parameters are not null.
/// </summary>
public class ValidateFrogAttribute : OverrideMethodAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IMethod> builder )
    {
        base.BuildEligibility( builder );

        // Skip static methods (will create skipped instances).
        builder.MustNotBeStatic();
    }

    public override dynamic? OverrideMethod()
    {
        foreach ( var param in meta.Target.Parameters )
        {
            if ( param.Type.IsConvertibleTo( typeof(Frog) ) && param.Value == null )
            {
                throw new ArgumentNullException( param.Name, "Frog cannot be null!" );
            }
        }
        return meta.Proceed();
    }
}
