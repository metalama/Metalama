// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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