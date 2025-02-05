// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Introspection;

public readonly struct IntrospectionReferenceDetail : IDiagnosticLocation
{
    public IIntrospectionReference Reference { get; }

    public ReferenceKinds Kinds { get; }

    public SourceReference Source { get; }

    internal IntrospectionReferenceDetail( IIntrospectionReference reference, ReferenceKinds kinds, SourceReference source )
    {
        this.Reference = reference;
        this.Kinds = kinds;
        this.Source = source;
    }
}