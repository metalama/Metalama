// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace;

/// <summary>
/// Creates instances of the <see cref="IFlashtraceLogger"/> interface. An instance of this interface must be registered into the <see cref="IServiceProvider"/>
/// or, if you are using the legacy mechanism, to <see cref="FlashtraceSourceFactory"/>.
/// </summary>
[PublicAPI]
public interface IFlashtraceRoleLoggerFactory
{
    /// <summary>
    /// Gets an instance of the <see cref="IFlashtraceLogger"/> for a specific <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The type of the source code that will emit the records.</param>
    /// <returns>An instance of the <see cref="IFlashtraceLogger"/> interface for <paramref name="type"/>.</returns>
    IFlashtraceLogger GetLogger( Type type );

    /// <summary>
    /// Gets an instance of the <see cref="IFlashtraceLogger"/> interface for a specific <paramref name="sourceName"/>. The name will
    /// usually, but not always, be a type name.
    /// </summary>
    /// <param name="sourceName">Name identifying the returned logger. The backend creates a logger based on this name.</param>
    IFlashtraceLogger GetLogger( string sourceName );
}