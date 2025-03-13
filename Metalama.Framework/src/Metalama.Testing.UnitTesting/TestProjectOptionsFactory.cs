// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Compiler;
using Metalama.Framework.Engine.Options;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Testing.UnitTesting
{
    internal sealed class TestProjectOptionsFactory : IProjectOptionsFactory
    {
        private readonly IProjectOptions _projectOptions;

        public TestProjectOptionsFactory( IProjectOptions projectOptions )
        {
            this._projectOptions = projectOptions;
        }

        public IProjectOptions GetProjectOptions( AnalyzerConfigOptions options, TransformerOptions? transformerOptions = null ) => this._projectOptions;
    }
}