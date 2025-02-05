using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Method_Abstract;

internal class NotNullAttribute : ContractAspect
{
    public override void Validate( dynamic? value )
    {
        if (value == null)
        {
            throw new ArgumentNullException( meta.Target.Parameter.Name );
        }
    }
}

// <target>
internal abstract class Base
{
    public abstract void M( [NotNull] string m );
}

// <target>
internal class Target : Base
{
    public override void M( string m ) { }
}