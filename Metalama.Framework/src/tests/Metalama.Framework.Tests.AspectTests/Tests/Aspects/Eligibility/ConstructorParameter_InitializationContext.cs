// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.ConstructorParameter_InitializationContext;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(TheAspect), typeof(MethodParameterAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Eligibility.ConstructorParameter_InitializationContext;

/// <summary>
/// Adds an initializer that runs after the last instance constructor, which appends an <c>InitializationContext</c> parameter to the constructor.
/// </summary>
public class TheAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
        => builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );

    [Template]
    private void InitializerTemplate() => Console.WriteLine( "OnConstructed" );
}

/// <summary>
/// A parameter aspect that is eligible only on parameters of methods, so it is not eligible on a constructor parameter.
/// </summary>
public class MethodParameterAspect : ParameterAspect
{
    public override void BuildEligibility( IEligibilityBuilder<IParameter> builder )
        => builder.DeclaringMember().MustBeInstanceOfType( typeof(IMethod) );
}

// <target>
[TheAspect]
public class Folder
{
    public Folder( [MethodParameterAspect] string name ) { }
}
