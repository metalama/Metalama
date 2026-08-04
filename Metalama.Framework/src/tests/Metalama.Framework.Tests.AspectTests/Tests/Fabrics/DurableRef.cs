// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;

namespace Metalama.Framework.Tests.PublicPipeline.Aspects.Fabrics.DurableRef
{
    /// <summary>
    /// Verifies that compile-time user code can call <c>ToDurableRef</c> and <c>ToDurable</c> and store the resulting
    /// <see cref="IDurableRef{T}"/>, which is what issue #1806 made possible. A fabric keeps its declarations for as
    /// long as the design-time pipeline keeps the fabric, so a non-durable reference here would retain a compilation.
    /// </summary>
    /// <remarks>
    /// The test is that this file compiles: the field type and both calls are part of the public compile-time API.
    /// </remarks>
    internal class Fabric : ProjectFabric
    {
        private readonly List<IDurableRef<INamedType>> _types = new();

        public override void AmendProject( IProjectAmender amender )
        {
            amender.SelectTypes()
                .Where(
                    t =>
                    {
                        // The element type of the list is the point of the test: the call compiles only because
                        // ToDurableRef returns the strongly-typed durable reference, and not a plain IRef that would
                        // have to be trusted rather than checked.
                        this._types.Add( t.ToDurableRef() );

                        return t.Name == nameof(TargetCode);
                    } )
                .AddAspect<Aspect>();
        }
    }

    internal class Aspect : TypeAspect
    {
        public override void BuildAspect( IAspectBuilder<INamedType> builder )
        {
            // A durable reference resolves through the same GetTarget as any other reference.
            var durableRef = builder.Target.ToRef().ToDurable();

            if ( !durableRef.IsDurable )
            {
                throw new InvalidOperationException( "The reference should have been durable." );
            }

            base.BuildAspect( builder );
        }

        [Introduce]
        public string ResolvedName => nameof(TargetCode);
    }

    // <target>
    internal class TargetCode { }
}
