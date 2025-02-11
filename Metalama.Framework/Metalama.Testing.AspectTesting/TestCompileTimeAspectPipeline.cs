// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Pipeline.CompileTime;
using Metalama.Framework.Engine.Services;

namespace Metalama.Testing.AspectTesting;

internal sealed class TestCompileTimeAspectPipeline : CompileTimeAspectPipeline
{
    public TestCompileTimeAspectPipeline( ProjectServiceProvider serviceProvider ) : base( serviceProvider ) { }

    protected override IDiagnosticExtensionPolicy GetDiagnosticExtensionPolicy()
        => ConstantDiagnosticExtensionPolicy.PropertiesOnly;
}