// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Metalama.Framework.Options;

/// <summary>
/// Context for the <see cref="IHierarchicalOptionsProvider"/>.<see cref="IHierarchicalOptionsProvider.GetOptions"/> method.
/// </summary>
[CompileTime]
public readonly struct OptionsProviderContext
{
    public IDeclaration TargetDeclaration { get; }

    public ScopedDiagnosticSink Diagnostics { get; }

    internal OptionsProviderContext( IDeclaration targetDeclaration, in ScopedDiagnosticSink diagnostics )
    {
        this.TargetDeclaration = targetDeclaration;
        this.Diagnostics = diagnostics;
    }
}