using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33144;

public sealed class NotNullAttribute : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if (value == null)
        {
            throw new ArgumentNullException();
        }
    }
}

// <target>
public class Class1
{
    public static Class1 operator +( [NotNull] Class1 left, int? right ) => new();

    public static Class1 operator +( int? left, [NotNull] Class1 right ) => new();
}