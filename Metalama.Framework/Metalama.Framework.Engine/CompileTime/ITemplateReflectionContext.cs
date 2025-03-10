// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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