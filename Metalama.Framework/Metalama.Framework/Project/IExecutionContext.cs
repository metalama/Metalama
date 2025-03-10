// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Services;
using Metalama.Framework.Utilities;
using System;

namespace Metalama.Framework.Project
{
    /// <summary>
    /// Represents the execution context of Metalama. Exposed by the <see cref="MetalamaExecutionContext.Current"/> property of <see cref="MetalamaExecutionContext"/>.
    /// </summary>
    [InternalImplement]
    [CompileTime]
    public interface IExecutionContext
    {
        /// <summary>
        /// Gets the <see cref="IServiceProvider"/>.
        /// </summary>
        IServiceProvider<IProjectService> ServiceProvider { get; }

        /// <summary>
        /// Gets the <see cref="IFormatProvider"/>, used to format formattable strings. This format provider will properly format elements of the code model
        /// and other similar objects.
        /// </summary>
        IFormatProvider FormatProvider { get; }

        /// <summary>
        /// Gets the current compilation.
        /// </summary>
        ICompilation Compilation { get; }

        /// <summary>
        /// Gets information about why the current code is executed and what are the abilities of the current execution context.
        /// </summary>
        IExecutionScenario ExecutionScenario { get; }

        /// <summary>
        /// Disables the mechanism of dependency collection in the current execution context.
        /// </summary>
        /// <returns>A cookie that must be restored to restore the execution context to its original state.</returns>
        IDisposable WithoutDependencyCollection();
    }

    internal interface IExecutionContextInternal : IExecutionContext
    {
        ISyntaxBuilderImpl? SyntaxBuilder { get; }

        IMetaApi? MetaApi { get; }
    }
}