// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.IntroducedMembers;
using System.Collections.Generic;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(NopAspect), typeof(IntroduceMembersAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Inheritance.IntroducedMembers;

[Inheritable]
internal class NopAspect : IAspect<IDeclaration>
{
    public void BuildAspect( IAspectBuilder<IDeclaration> builder ) { }

    public void BuildEligibility( IEligibilityBuilder<IDeclaration> builder ) { }
}

internal class IntroduceMembersAttribute : TypeAspect
{
    [Template]
    public virtual int M<T>( (int x, int y) p ) => p.x;

    [Template]
    public virtual event EventHandler? Event;

    [Template]
    public virtual int Property { get; set; }

    [Template]
    public int IndexerGet( int i ) => 0;

    [Template]
    public void IndexerSet( int i, int value ) { }

    [Template]
    public virtual void Finalizer() { }

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        var results = new List<IIntroductionAdviceResult<IDeclaration>>();

        results.Add( builder.IntroduceMethod( nameof(M) ) );
        results.Add( builder.IntroduceEvent( nameof(Event) ) );
        results.Add( builder.IntroduceProperty( nameof(Property) ) );

        results.Add(
            builder.IntroduceIndexer(
                typeof(int),
                nameof(IndexerGet),
                nameof(IndexerSet),
                buildIndexer: builder => builder.IsVirtual = true ) );

        results.Add( builder.IntroduceFinalizer( nameof(Finalizer) ) );

        foreach (var result in results)
        {
            result.AddAspect<NopAspect>();

            if (result.Declaration is IMethod method)
            {
                if (!method.ReturnType.IsConvertibleTo( typeof(void) ))
                {
                    builder.With( method.ReturnParameter ).AddAspect<NopAspect>();
                }
            }

            if (result.Declaration is IHasParameters hasParameters)
            {
                foreach (var param in hasParameters.Parameters)
                {
                    builder.With( param ).AddAspect<NopAspect>();
                }
            }

            if (result.Declaration is IHasAccessors member)
            {
                foreach (var accessor in member.Accessors)
                {
                    builder.With( accessor ).AddAspect<NopAspect>();
                }
            }
        }
    }
}

[IntroduceMembers]
public class C { }