// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System;
using System.Threading;

namespace Metalama.Framework.Project
{
    /// <summary>
    /// Exposes the current execution context of Metalama.
    /// </summary>
    [CompileTime]
    public static class MetalamaExecutionContext
    {
        private static readonly AsyncLocal<IExecutionContextInternal?> _current = new();

        /// <summary>
        /// Gets the current execution context, or throws an exception if there no execution context.
        /// </summary>
        public static IExecutionContext Current => CurrentInternal;

        internal static IExecutionContextInternal CurrentInternal
            => _current.Value ?? throw new InvalidOperationException( $"The {nameof(MetalamaExecutionContext)} is not available." );

        /// <summary>
        /// Gets or sets the current execution context, or <c>null</c> if there is no execution context.
        /// </summary>
        internal static IExecutionContextInternal? CurrentOrNull
        {
            get => _current.Value;
            set => _current.Value = value;
        }
    }
}