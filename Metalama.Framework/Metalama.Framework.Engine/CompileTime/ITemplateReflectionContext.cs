// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.CompileTime;

internal interface ITemplateReflectionContext
{
    Compilation Compilation { get; }

    CompilationContext CompilationContext { get; }

    CompilationModel GetCompilationModel( ICompilation sourceCompilation );

    bool IsCacheable { get; }
}