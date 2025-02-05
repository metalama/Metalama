// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Metalama.Framework.Engine.Options;

public interface IProjectOptionsFactory : IGlobalService
{
    IProjectOptions GetProjectOptions( AnalyzerConfigOptions options, TransformerOptions? transformerOptions = null );
}