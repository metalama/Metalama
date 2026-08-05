// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Extensions.DependencyInjection;
using Metalama.Extensions.DependencyInjection.AspectTests.Bugs.IntroducedTypeDependency;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(DependencyAttribute), typeof(IntroducedTypeDependencyAspect) )]

namespace Metalama.Extensions.DependencyInjection.AspectTests.Bugs.IntroducedTypeDependency;

/// <summary>
/// An aspect that introduces a type into a namespace of its own and then introduces, into its own target, a dependency
/// whose type is that introduced type.
/// </summary>
/// <remarks>
/// This is the shape of the <c>ImplementMetricsAspect</c> of the metrics sample. The pull strategy makes the type of
/// the constructor parameter durable, and neither the namespace nor the type exists in the source, so resolving that
/// durable reference requires the lookup to reach the declarations that the aspect has introduced. See issue #1825.
/// </remarks>
public class IntroducedTypeDependencyAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var introducedType = builder
            .With( builder.Target.Compilation )
            .WithNamespace( "Introduced" )
            .IntroduceClass(
                builder.Target.Name + "Companion",
                buildType: t => t.Accessibility = Accessibility.Public );

        builder.IntroduceDependency(
            introducedType.Declaration.ToNullable(),
            new DependencyOptions { IsRequired = false } );
    }
}

// <target>
[IntroducedTypeDependencyAspect]
public class TargetClass
{
    public TargetClass() { }
}
