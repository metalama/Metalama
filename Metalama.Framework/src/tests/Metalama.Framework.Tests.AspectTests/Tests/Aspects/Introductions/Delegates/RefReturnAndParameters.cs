// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.Delegates.RefReturnAndParameters;

public class IntroductionAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // A delegate may return by reference, and its parameters may carry any reference kind, so both are
        // expressed rather than refused.
        builder.IntroduceDelegate(
            "RefReturning",
            d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Int32 );
                d.ReturnParameter.RefKind = RefKind.Ref;
                d.AddParameter( "storage", typeof(int[]) );
            } );

        builder.IntroduceDelegate(
            "RefReadOnlyReturning",
            d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Int32 );
                d.ReturnParameter.RefKind = RefKind.RefReadOnly;
                d.AddParameter( "storage", typeof(int[]) );
            } );

        builder.IntroduceDelegate(
            "WithRefParameters",
            d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Void );
                d.AddParameter( "byRef", TypeFactory.GetType( SpecialType.Int32 ), RefKind.Ref );
                d.AddParameter( "byOut", TypeFactory.GetType( SpecialType.Int32 ), RefKind.Out );
                d.AddParameter( "byIn", TypeFactory.GetType( SpecialType.Int32 ), RefKind.In );
            } );

        builder.IntroduceDelegate(
            "WithDefaultValue",
            d =>
            {
                d.Accessibility = Accessibility.Public;
                d.ReturnType = TypeFactory.GetType( SpecialType.Void );
                d.AddParameter( "value", TypeFactory.GetType( SpecialType.Int32 ), RefKind.None, TypedConstant.Create( 42 ) );
            } );
    }
}

// <target>
[Introduction]
public class TargetType { }
