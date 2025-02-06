// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Extensions to the <see cref="LogRecordKind"/> enum.
/// </summary>
[PublicAPI]
public static class LogRecordKindExtensions
{
    /// <summary>
    /// Determines whether a given <see cref="LogRecordKind"/> represents the opening of a context.
    /// </summary>
    /// <param name="kind">A <see cref="LogRecordKind"/>.</param>
    /// <returns><c>true</c> if <paramref name="kind"/> represents the opening of a context, otherwise <c>false</c>.</returns>
    public static bool IsOpen( this LogRecordKind kind )
    {
        switch ( kind )
        {
            case LogRecordKind.MethodEntry:
            case LogRecordKind.AsyncMethodResume:
            case LogRecordKind.ActivityEntry:
            case LogRecordKind.IteratorMoveNext:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Determines whether a given <see cref="LogRecordKind"/> represents the closing of a context.
    /// </summary>
    /// <param name="kind">A <see cref="LogRecordKind"/>.</param>
    /// <returns><c>true</c> if <paramref name="kind"/> represents the closing of a context, otherwise <c>false</c>.</returns>
    public static bool IsClose( this LogRecordKind kind )
    {
        switch ( kind )
        {
            case LogRecordKind.MethodSuccess:
            case LogRecordKind.MethodOvertime:
            case LogRecordKind.MethodException:
            case LogRecordKind.AsyncMethodAwait:
            case LogRecordKind.IteratorYield:
            case LogRecordKind.ActivityExit:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Determines whether a given <see cref="LogRecordKind"/> represents the closing of an activity.
    /// </summary>
    /// <param name="kind">A <see cref="LogRecordKind"/>.</param>
    /// <returns><c>true</c> if <paramref name="kind"/> represents the closing of an activity, otherwise <c>false</c>.</returns>
    public static bool IsCloseActivity( this LogRecordKind kind )
    {
        switch ( kind )
        {
            case LogRecordKind.ActivityExit:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Determines whether a given <see cref="LogRecordKind"/> represents a standalone record, i.e. a record that does
    /// not have a corresponding opening or closing. For instance, a <see cref="LogRecordKind.Message"/>
    /// is a standalone record.
    /// </summary>
    /// <param name="kind">A <see cref="LogRecordKind"/>.</param>
    /// <returns><c>true</c> if <paramref name="kind"/> is a standalone record, otherwise <c>false</c>.</returns>
    /// <remarks>
    /// <para><seealso cref="LogRecordKind.MethodException"/> may represent an closing or a standalone record, depending on
    /// the context. This method shall return <c>false</c> for <seealso cref="LogRecordKind.MethodException"/>.</para>
    /// </remarks>
    public static bool IsStandalone( this LogRecordKind kind )
    {
        switch ( kind )
        {
            case LogRecordKind.Message:
            case LogRecordKind.ExecutionPoint:
            case LogRecordKind.ValueChanged:
                return true;

            default:
                return false;
        }
    }

    // TODO: [FT-Review] Review use of method name 'IsUser' here. The method was previously named 'IsCustom'.

    /// <summary>
    /// Determines whether the current <see cref="LogRecordKind"/> represents a record emitted by the
    /// <see cref="FlashtraceLevelSource"/> class, which is typically invoked by user code.
    /// </summary>
    /// <param name="kind">A <see cref="LogRecordKind"/>.</param>
    /// <returns><c>true</c> if <paramref name="kind"/> is a record emitted by the <see cref="FlashtraceLevelSource"/> class,
    /// otherwise <c>false</c>.</returns>
    public static bool IsUser( this LogRecordKind kind )
    {
        switch ( kind )
        {
            case LogRecordKind.Message:
            case LogRecordKind.ActivityEntry:
            case LogRecordKind.ActivityExit:
            case LogRecordKind.ExecutionPoint:
                return true;

            default:
                return false;
        }
    }
}