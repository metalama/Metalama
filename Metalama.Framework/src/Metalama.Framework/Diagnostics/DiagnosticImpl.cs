// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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