// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.AspectOrdering;

/// <summary>
/// An <see cref="IAspectOrderingSource"/> that orders aspects provided by <c>Metalama.Framework</c> before the other.
/// </summary>
internal sealed class FrameworkAspectOrderingSource : IAspectOrderingSource
{
    private readonly ImmutableArray<AspectClass> _aspectTypes;

    public FrameworkAspectOrderingSource( ImmutableArray<AspectClass> aspectTypes )
    {
        this._aspectTypes = aspectTypes;
    }

    public IEnumerable<AspectOrderSpecification> GetAspectOrderSpecification( IDiagnosticAdder diagnosticAdder )
    {
        static bool IsFrameworkAspect( AspectClass t ) => t.Project?.RunTimeIdentity.Name == "Metalama.Framework";

        var frameworkAspects = this._aspectTypes.Where( IsFrameworkAspect ).ToReadOnlyList();

        foreach ( var aspectClass in this._aspectTypes )
        {
            if ( !IsFrameworkAspect( aspectClass ) )
            {
                foreach ( var frameworkAspect in frameworkAspects )
                {
                    yield return new AspectOrderSpecification( [frameworkAspect.FullName, aspectClass.FullName], false );
                }
            }
        }
    }
}