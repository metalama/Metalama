// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Represents an instance of <see cref="DiagnosticDefinition{T}"/>, encapsulating the arguments used for the parametric diagnostic definition.
/// </summary>
internal sealed class DiagnosticImpl<T> : IDiagnostic
    where T : notnull
{
    private readonly DiagnosticDefinition<T> _definition;
    private readonly T _arguments;
    private ImmutableArray<IDiagnosticExtension> _extensions;

    object IDiagnostic.Arguments => this._arguments;

    public IDiagnostic WithExtensions( ImmutableArray<IDiagnosticExtension> extensions )
    {
        this._extensions = this._extensions.AddRange( extensions );

        return this;
    }

    IDiagnosticDefinition IDiagnostic.Definition => this._definition;

    ImmutableArray<IDiagnosticExtension> IDiagnostic.Extensions => this._extensions;

    public DiagnosticImpl( DiagnosticDefinition<T> definition, T arguments, ImmutableArray<IDiagnosticExtension> extensions )
    {
        this._definition = definition;
        this._arguments = arguments;
        this._extensions = extensions;
    }

    public override string ToString() => $"{this._definition.Id}: {this._definition.Title} ({this._arguments})";
}