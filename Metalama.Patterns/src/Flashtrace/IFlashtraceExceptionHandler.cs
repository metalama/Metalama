// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Flashtrace.Contexts;
using JetBrains.Annotations;

namespace Flashtrace;

/// <summary>
/// Defines methods called in case of exception in logging infrastructure classes. This interface
/// can be implemented by any class implementing the <see cref="IFlashtraceLogger"/> interface.
/// When an <see cref="IFlashtraceLogger"/> does not implement this interface, logging exceptions are silently ignored.
/// </summary>
[PublicAPI]
public interface IFlashtraceExceptionHandler
{
    /// <summary>
    /// Method invoked when the user code calling a logging infrastructure method is invalid, e.g. when the formatting string
    /// is incorrect or does not match the arguments.
    /// </summary>
    /// <param name="callerInfo">Information about the line of code causing the error.</param>
    /// <param name="format">Formatting string of the error message.</param>
    /// <param name="args">Arguments.</param>
    void OnInvalidUserCode( in CallerInfo callerInfo, string format, params object[] args );

    /// <summary>
    /// Method invoked when an exception is thrown in logging code.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/>.</param>
    void OnInternalException( Exception exception );
}