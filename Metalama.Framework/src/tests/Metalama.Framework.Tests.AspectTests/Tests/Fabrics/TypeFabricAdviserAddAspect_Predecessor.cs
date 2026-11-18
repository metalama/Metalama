// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

// The predecessor of a child aspect added through the adviser of a type fabric must be the fabric instance, as with amender.

namespace Metalama.Framework.Tests.AspectTests.Aspects.Fabrics.TypeFabricAdviserAddAspect_Predecessor;

[CompileTime]
internal static class PredecessorDescriber
{
    public static string Describe( IAspectInstance aspectInstance )
        => string.Join(
            ", ",
            aspectInstance.Predecessors.Select(
                p => p.Kind + ":" + p.Instance switch
                {
                    IFabricInstance fabricInstance => "Fabric " + fabricInstance.Fabric.GetType().Name,
                    IAspectInstance parentAspectInstance => "Aspect " + parentAspectInstance.AspectClass.ShortName,
                    _ => p.Instance.GetType().Name
                } ) );
}

internal class MethodAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( PredecessorDescriber.Describe( meta.AspectInstance ) );

        return meta.Proceed();
    }
}

internal class IntroducingAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.IntroduceMethod( nameof(Describe), args: new { description = PredecessorDescriber.Describe( builder.AspectInstance ) } );
    }

    [Template]
    private void Describe( [CompileTime] string description )
    {
        Console.WriteLine( description );
    }
}

// <target>
internal class TargetCode
{
    private class Fabric : TypeFabric
    {
        public override void AmendType( ITypeAmender amender )
        {
            // Through the adviser of the type fabric.
            amender.AddAspect<IntroducingAspect>();
            amender.With( amender.Type.Methods.OfName( nameof(AddedThroughAdviser) ).Single() ).AddAspect<MethodAspect>();
            amender.With( amender.Type.Methods.OfName( nameof(RequiredThroughAdviser) ).Single() ).RequireAspect<MethodAspect>();

            // Through the query of the type fabric, for comparison.
            amender.SelectMany( t => t.Methods.OfName( nameof(AddedThroughQuery) ) ).AddAspect<MethodAspect>();
            amender.SelectMany( t => t.Methods.OfName( nameof(RequiredThroughQuery) ) ).RequireAspect<MethodAspect>();
        }
    }

    private void AddedThroughAdviser() { }

    private void RequiredThroughAdviser() { }

    private void AddedThroughQuery() { }

    private void RequiredThroughQuery() { }
}
