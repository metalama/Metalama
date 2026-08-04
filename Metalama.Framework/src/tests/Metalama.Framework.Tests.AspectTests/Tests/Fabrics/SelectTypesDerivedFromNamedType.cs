// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using System;

// The overload of SelectTypesDerivedFrom that takes an INamedType, rather than a System.Type, keeps that type as a
// durable reference, because the query belongs to the pipeline configuration and therefore outlives the compilation
// the fabric ran in (issue #1799). The durable form is a SerializableTypeId and not the SerializableDeclarationId that
// IRef.ToDurable produces, because a declaration identifier names the generic definition only.
//
// The expected output therefore has MethodOnPlain overridden and neither MethodOnInt nor MethodOnString. Going through
// a declaration identifier resolves BaseClass<int> to BaseClass<T> and overrides both of the latter, which is how this
// test fails if the distinction is lost.

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.SelectTypesDerivedFromNamedType
{
    internal class Fabric : ProjectFabric
    {
        public override void AmendProject( IProjectAmender amender )
        {
            amender
                .SelectTypesDerivedFrom( (INamedType)TypeFactory.GetType( typeof(PlainBaseClass) ) )
                .SelectMany( t => t.Methods )
                .AddAspect<Aspect>();

            amender
                .SelectTypesDerivedFrom( (INamedType)TypeFactory.GetType( typeof(BaseClass<int>) ) )
                .SelectMany( t => t.Methods )
                .AddAspect<Aspect>();
        }
    }

    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod()
        {
            Console.WriteLine( "overridden" );

            return meta.Proceed();
        }
    }

    internal class PlainBaseClass { }

    internal class BaseClass<T> { }

    // <target>
    internal class TargetCode
    {
        internal class DerivedFromPlain : PlainBaseClass
        {
            public void MethodOnPlain() { }
        }

        internal class DerivedFromInt : BaseClass<int>
        {
            public void MethodOnInt() { }
        }

        internal class DerivedFromString : BaseClass<string>
        {
            public void MethodOnString() { }
        }
    }
}
