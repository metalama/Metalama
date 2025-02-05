// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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