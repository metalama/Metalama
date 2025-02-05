// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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