// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_Dependency_RunTimeDependencyFirst;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(DependencyAspect), typeof(InitializerAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Initialization.OnConstructed_Forwarder_Dependency_RunTimeDependencyFirst;

/// <summary>
/// Introduces an optional service parameter to every constructor, like the dependency injection aspects of Metalama.Extensions.
/// </summary>
public class DependencyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var type = builder.Target.Compilation.Factory.GetTypeByReflectionType( typeof(IFormatProvider) ).ToNullable();

        foreach ( var constructor in builder.Target.Constructors.Where( c => c.InitializerKind != ConstructorInitializerKind.This ) )
        {
            builder.With( constructor )
                .IntroduceParameter(
                    "formatProvider",
                    type,
                    TypedConstant.Default( type ),
                    PullStrategy.IntroduceParameterAndPull(
                        "formatProvider",
                        type,
                        TypedConstant.Default( type ),
                        reuseExistingParameterOfCompatibleType: true ) );
        }
    }
}

public class InitializerAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.AddInitializer( nameof(InitializerTemplate), InitializerKind.AfterLastInstanceConstructor );
    }

    [Template]
    private void InitializerTemplate()
    {
        Console.WriteLine( $"OnConstructed {meta.Target.Type.Name}" );
    }
}

// <target>
[DependencyAspect]
[InitializerAspect]
public class ImplicitConstructor
{
}

// <target>
[DependencyAspect]
[InitializerAspect]
public class ExplicitConstructors
{
    public ExplicitConstructors() { }

    public ExplicitConstructors( int x ) { }
}

internal class Program
{
    public static void TestMain()
    {
        foreach ( var type in new[] { typeof(ImplicitConstructor), typeof(ExplicitConstructors) } )
        {
            foreach ( var constructor in type.GetConstructors().OrderBy( c => c.ToString() ) )
            {
                Console.WriteLine( $"{type.Name}: {constructor}" );
            }
        }

        _ = new ImplicitConstructor();
        _ = new ExplicitConstructors();
    }
}
