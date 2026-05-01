// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.DesignTime.Contracts.AspectExplorer;

[Obsolete]
[PublicAPI]
[Guid( "22139B0D-3397-4210-8F85-374A2D543DB0" )]
public struct AspectExplorerAspectInstance
{
    public ISymbol TargetDeclaration;
    public AspectExplorerDeclarationKind TargetDeclarationKind;
    public AspectExplorerAspectTransformation[] Transformations;
}

[PublicAPI]
[ComImport]
[Guid( "8A11CA4E-B57C-4A3F-861D-5BA93209D55A" )]
public interface IAspectExplorerAspectInstance
{
    ISymbol TargetDeclaration { get; }

    AspectExplorerDeclarationKind TargetDeclarationKind { get; }

    IAspectExplorerAspectTransformation[] Transformations { get; }
}

[Obsolete]
[PublicAPI]
[Guid( "C35DF59E-9359-4D8B-AF8A-6DA4F5540F00" )]
public struct AspectExplorerAspectTransformation
{
    public ISymbol TargetDeclaration;
    public AspectExplorerDeclarationKind TargetDeclarationKind;
    public string Description;
}

[PublicAPI]
[ComImport]
[Guid( "AFB6A223-4B40-4F75-B88D-01F1228A5187" )]
public interface IAspectExplorerAspectTransformation
{
    ISymbol TargetDeclaration { get; }

    AspectExplorerDeclarationKind TargetDeclarationKind { get; }

    string Description { get; }

    ISymbol? TransformedDeclaration { get; }

    string? FilePath { get; }
}

[PublicAPI]
[Guid( "96F4689F-0FBA-4732-B7C3-069F608F79C2" )]
public enum AspectExplorerDeclarationKind
{
    Default,
    ReturnParameter
}