// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Fabrics;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability.AspectTests.Options.IgnoreUnobservableExpressions.UsingFabricOnOtherClassInProject;

public sealed class Fabric : ProjectFabric
{
    public override void AmendProject( IProjectAmender amender )
    {
        amender
            .SelectReflectionType( typeof(OtherClass) )
            .ConfigureObservability( b => b.ObservabilityContract = ObservabilityContract.Constant );
    }
}

// ReSharper disable once MemberCanBeInternal
public static class OtherClass
{
    public static int Foo() => 42;
}

// <target>
[Observable]
public class UsingFabricOnOtherClassInProject
{
    public int X => OtherClass.Foo();
}