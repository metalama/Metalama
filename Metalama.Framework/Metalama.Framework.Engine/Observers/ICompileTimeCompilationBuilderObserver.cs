// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Observers
{
    /// <summary>
    /// An interface that can be injected into the service provider to get callbacks from the <see cref="CompileTimeCompilationBuilder"/>
    /// class. For testing only.
    /// </summary>
    public interface ICompileTimeCompilationBuilderObserver : IProjectService
    {
        /// <summary>
        /// Method called by <see cref="CompileTimeCompilationBuilder.TryCreateCompileTimeCompilation"/>.
        /// </summary>
        void OnCompileTimeCompilation( Compilation compilation, IReadOnlyDictionary<string, string> compileTimeToSourceMap );

        void OnCompileTimeCompilationEmit( ImmutableArray<Diagnostic> diagnostics );
    }
}