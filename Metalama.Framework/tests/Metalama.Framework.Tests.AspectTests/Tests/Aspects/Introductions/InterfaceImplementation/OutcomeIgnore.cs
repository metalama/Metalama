using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.OutcomeIgnore;

public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        if (builder.ImplementInterface( typeof(IDisposable), whenExists: OverrideStrategy.Ignore ).Outcome != AdviceOutcome.Ignore)
        {
            // We should not get there.
            throw new InvalidOperationException();
        }
    }

    [InterfaceMember]
    public void Dispose() { }
}

// <target>
[TheAspect]
internal class C : IDisposable
{
    public void Dispose() { }
}