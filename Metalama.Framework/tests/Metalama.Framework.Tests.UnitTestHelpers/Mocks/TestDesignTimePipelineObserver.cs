// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Pipeline;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTestHelpers.Mocks;

public sealed class TestDesignTimePipelineObserver : IDesignTimeAspectPipelineObserver
{
    public List<string> InitializePipelineEvents { get; } = new();

    public void OnInitializePipeline( Compilation compilation ) => this.InitializePipelineEvents.Add( compilation.AssemblyName! );
}