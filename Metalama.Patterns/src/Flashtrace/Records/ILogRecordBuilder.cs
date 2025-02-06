// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

// TODO: Consider normalizing the terms "Log Record", "Log Event" and "Log Entry" in type names etc, likely to "Log Record" et al. 

namespace Flashtrace.Records;

/// <summary>
/// Allows to build a log record (typically, but not necessarily, a string). A log record can be composed of one of several
/// items.
/// </summary>
[PublicAPI]
public interface ILogRecordBuilder : IDisposable
{
    /// <summary>
    /// Begins to build a specified item.
    /// </summary>
    /// <param name="item">The item being built.</param>
    /// <param name="options">Options.</param>
    void BeginWriteItem( LogRecordItem item, in LogRecordTextOptions options );

    /// <summary>
    /// Ends building a specified item.
    /// </summary>
    /// <param name="item"></param>
    void EndWriteItem( LogRecordItem item );

    /// <summary>
    /// Writes a parameter.
    /// </summary>
    /// <typeparam name="T">Type of the parameter value.</typeparam>
    /// <param name="index">Index of the parameter (zero-based).</param>
    /// <param name="parameterName">Name of the parameter.</param>
    /// <param name="value">Value of the parameter.</param>
    /// <param name="options">Options.</param>
    void WriteParameter<T>( int index, in ReadOnlySpan<char> parameterName, T? value, in LogParameterOptions options );

    /// <summary>
    /// Writes a string.
    /// </summary>
    /// <param name="str">A string.</param>
    void WriteString( in ReadOnlySpan<char> str );

    /// <summary>
    /// Assigns an <see cref="Exception"/> to the record.
    /// </summary>
    /// <param name="exception">The <see cref="Exception"/>.</param>
    void SetException( Exception exception );

    /// <summary>
    /// Sets an execution time to the record.
    /// </summary>
    /// <param name="executionTime">Execution time.</param>
    /// <param name="isOvertime"><c>true</c> whether the activity is overtime, otherwise <c>false</c>.</param>
    void SetExecutionTime( double executionTime, bool isOvertime );

    /// <summary>
    /// Completes the creation of the record. It must be invoked before <see cref="IDisposable.Dispose"/>.
    /// </summary>
    void Complete();
}