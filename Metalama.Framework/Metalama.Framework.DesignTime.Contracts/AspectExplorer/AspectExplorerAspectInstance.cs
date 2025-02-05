// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.AspectExplorer;

[Obsolete]
[PublicAPI]
[Guid( "AF977B33-AA8C-4481-9B7E-B14A67200429" )]
public struct AspectExplorerAspectInstance
{
    public ISymbol TargetDeclaration;
    public AspectExplorerDeclarationKind TargetDeclarationKind;
    public AspectExplorerAspectTransformation[] Transformations;
}

[PublicAPI]
[ComImport]
[Guid( "415F68C2-FFAD-4176-9062-53C3658E5F18" )]
public interface IAspectExplorerAspectInstance
{
    public ISymbol TargetDeclaration { get; }

    public AspectExplorerDeclarationKind TargetDeclarationKind { get; }

    public IAspectExplorerAspectTransformation[] Transformations { get; }
}

[Obsolete]
[PublicAPI]
[Guid( "E758C91B-E335-4D53-AA30-82BBCCBF428A" )]
public struct AspectExplorerAspectTransformation
{
    public ISymbol TargetDeclaration;
    public AspectExplorerDeclarationKind TargetDeclarationKind;
    public string Description;
}

[PublicAPI]
[ComImport]
[Guid( "E0C881D8-C8FF-4988-B73D-CDEB6561CEBD" )]
public interface IAspectExplorerAspectTransformation
{
    public ISymbol TargetDeclaration { get; }

    public AspectExplorerDeclarationKind TargetDeclarationKind { get; }

    public string Description { get; }

    public ISymbol? TransformedDeclaration { get; }

    public string? FilePath { get; }
}

[PublicAPI]
[Guid( "1E91F9F1-FD0E-4668-B4D1-6D445C7BE1FD" )]
public enum AspectExplorerDeclarationKind
{
    Default,
    ReturnParameter
}