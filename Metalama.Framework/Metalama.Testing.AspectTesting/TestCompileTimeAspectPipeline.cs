// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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