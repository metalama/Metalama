// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.Constructor_IntroducedParameter_OrCondition;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(TheAspect), typeof(StaticConstructorAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.Constructor_IntroducedParameter_OrCondition;

/// <summary>
/// Introduces a parameter into each constructor of the target type.
/// </summary>
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach ( var constructor in builder.Target.Constructors )
        {
            builder.With( constructor ).IntroduceParameter( "introduced", typeof(int), TypedConstant.Create( 0 ) );
        }
    }
}

/// <summary>
/// A constructor aspect that is eligible only on static or private constructors, so it is not eligible on a public instance constructor.
/// </summary>
public class StaticConstructorAspect : ConstructorAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IConstructor> builder )
        => builder.MustSatisfyAny(
            b => b.MustBeStatic(),
            b => b.MustSatisfy( c => c.Accessibility == Accessibility.Private, c => $"{c} must be private" ) );
}

// <target>
[TheAspect]
public class Folder
{
    [StaticConstructorAspect]
    public Folder( string name ) { }
}
