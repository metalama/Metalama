// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.IntegrationTests.Aspects.Introductions.Constructors.ThisInitializer;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceConstructor(
            nameof(Template),
            whenExists: OverrideStrategy.New,
            buildConstructor: c =>
            {
                c.InitializerKind = ConstructorInitializerKind.This;
                c.AddInitializerArgument( TypedConstant.Create( 13 ) );
                c.AddInitializerArgument( TypedConstant.Create( 42 ) );
            } );

        builder.IntroduceConstructor(
            nameof(Template),
            buildConstructor: c =>
            {
                var p = c.AddParameter( "p", typeof(int) );
                c.InitializerKind = ConstructorInitializerKind.This;
                c.AddInitializerArgument( p );
                c.AddInitializerArgument( TypedConstant.Create( 42 ) );
            } );
    }

    [Template]
    public void Template() { }
}

// <target>
[Introduction]
internal class TargetClass
{
    public TargetClass( int x, int y ) { }
}