// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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