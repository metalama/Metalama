// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;

namespace Metalama.Testing.AspectTesting;

internal sealed class TestCompileTimeAspectPipeline : CompileTimeAspectPipeline
{
    private readonly bool _collectCodeFixes;

    public TestCompileTimeAspectPipeline( ProjectServiceProvider serviceProvider, bool collectCodeFixes ) : base( serviceProvider )
    {
        this._collectCodeFixes = collectCodeFixes;
    }

    protected override IDiagnosticExtensionPolicy GetDiagnosticExtensionPolicy()
        => this._collectCodeFixes ? ConstantDiagnosticExtensionPolicy.PropertiesOnly : ConstantDiagnosticExtensionPolicy.None;
}