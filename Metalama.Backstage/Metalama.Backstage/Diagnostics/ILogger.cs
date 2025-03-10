// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Backstage.Diagnostics
{
    [PublicAPI]
    public interface ILogger
    {
        /// <summary>
        /// Gets the <see cref="ILogWriter"/> for the <c>Trace</c> severity. Messages of this severity are written only in verbose mode.
        /// </summary>
        ILogWriter? Trace { get; }

        /// <summary>
        /// Gets the <see cref="ILogWriter"/> for the <c>Info</c> severity. Messages of this severity are written to the console output as normal text,
        /// or in logs even when the verbosity for the category is not set to verbose.
        /// </summary>
        ILogWriter? Info { get; }

        /// <summary>
        /// Gets the <see cref="ILogWriter"/> for the <c>Warning</c> severity. Messages of this severity are written to the console output as warnings,
        /// and to the logs.
        /// </summary>
        ILogWriter? Warning { get; }

        /// <summary>
        /// Gets the <see cref="ILogWriter"/> for the <c>Errors</c> severity. Messages of this severity are written to the console output as errors,
        /// and to the logs.
        /// </summary>
        ILogWriter? Error { get; }

        /// <summary>
        /// Returns a new <see cref="ILogger"/> for a sub-category.
        /// </summary>
        ILogger WithPrefix( string prefix );
    }
}