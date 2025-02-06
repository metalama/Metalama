// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Flashtrace.Contexts;

/// <summary>
/// Describes the caller of a logging method.
/// </summary>
[PublicAPI]
public readonly struct CallerInfo
{
    private readonly Type? _sourceType;

    /// <summary>
    /// Initializes a new instance of the <see cref="CallerInfo"/> struct specifying the source type as a <see cref="RuntimeTypeHandle"/>.
    /// </summary>
    /// <param name="sourceTypeToken"><see cref="RuntimeTypeHandle"/> of the calling type.</param>
    /// <param name="methodName">Name of the calling method.</param>
    /// <param name="file">Path of the source code of the calling code.</param>
    /// <param name="line">Line in <paramref name="file"/> of the caller.</param>
    /// <param name="column">Column in <paramref name="file"/> of the caller.</param>
    [DebuggerStepThrough]
    public CallerInfo( RuntimeTypeHandle sourceTypeToken, string methodName, string file, int line, int column )
    {
        this.SourceTypeToken = sourceTypeToken;
        this.MethodName = methodName;
        this.SourceLineInfo = new SourceLineInfo( file, line, column );
        this._sourceType = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CallerInfo"/> struct specifying the source type as a <see cref="Type"/>.
    /// </summary>
    /// <param name="sourceType"><see cref="Type"/> of the calling type.</param>
    /// <param name="methodName">Name of the calling method.</param>
    /// <param name="file">Path of the source code of the calling code.</param>
    /// <param name="line">Line in <paramref name="file"/> of the caller.</param>
    /// <param name="column">Column in <paramref name="file"/> of the caller.</param>
    [DebuggerStepThrough]
    public CallerInfo( Type sourceType, string methodName, string? file, int line, int column )
    {
        this._sourceType = sourceType;
        this.MethodName = methodName;
        this.SourceTypeToken = default;
        this.SourceLineInfo = new SourceLineInfo( file, line, column );
    }

    /// <summary>
    /// Gets the source <see cref="Type"/>.
    /// </summary>
    public Type? SourceType => this._sourceType ?? (this.SourceTypeToken.Value == default ? null : Type.GetTypeFromHandle( this.SourceTypeToken ));

    /// <summary>
    /// Gets the <see cref="RuntimeTypeHandle"/> of the caller <see cref="Type"/>.
    /// </summary>
    public RuntimeTypeHandle SourceTypeToken { get; }

    /// <summary>
    /// Gets the name of the caller method.
    /// </summary>
    public string? MethodName { get; }

    /// <summary>
    /// Gets the <see cref="SourceLineInfo"/> of the caller.
    /// </summary>
    public SourceLineInfo SourceLineInfo { get; }

    /// <summary>
    /// Gets a value indicating whether the current <see cref="CallerInfo"/> is null.
    /// </summary>
    public bool IsNull => this.MethodName == null && this.SourceLineInfo.IsNull;

    internal static CallerInfo Null;

    /// <inheritdoc />
    public override string ToString()
    {
        if ( this.IsNull )
        {
            return "null";
        }

        var useTypeName = this.SourceType?.FullName ?? this.SourceType?.Name ?? "<unknown>";
        var useMethodName = this.MethodName ?? "<unknown>";

        if ( this.SourceLineInfo.IsNull )
        {
            return useTypeName + "." + useMethodName;
        }
        else
        {
            return useTypeName + "." + useMethodName + " at " + this.SourceLineInfo.File + ", " + this.SourceLineInfo.Line;
        }
    }

    [MethodImpl( MethodImplOptions.AggressiveInlining )]
    public CallerInfo GetDynamicWhenNull( int skipFrames = 0 ) => this.IsNull ? this : GetDynamic( skipFrames );

    /// <summary>
    /// Gets a <see cref="CallerInfo"/> of the caller by performing a stack walk (using <see cref="StackFrame"/>).
    /// </summary>
    /// <param name="skipFrames">The number of stack frames to skip.</param>
    /// <returns> A <see cref="CallerInfo"/> for the caller (skipping the specified number of stack frames), or <c>default</c> if the platform does not support the <see cref="StackFrame"/> class.</returns>
    public static CallerInfo GetDynamic( int skipFrames )
    {
        // ReSharper code cleanup wants ';;', but this leads to a missing space warning which must be disabled.
#pragma warning disable SA1002
        for ( var i = skipFrames + 1;; i++ )
#pragma warning restore SA1002
        {
            var frame = new StackFrame( i, true );

            var method = frame.GetMethod();

            if ( method == null )
            {
                // We reach the bottom of the stack.
                return default;
            }

            if ( method.DeclaringType == null )
            {
                continue;
            }

            // TODO: Do we want to automagically skip any Flashtrace or Metalama stack frames here?

            return new CallerInfo(
                method.DeclaringType,
                method.Name,
                frame.GetFileName(),
                frame.GetFileLineNumber(),
                frame.GetFileColumnNumber() );
        }
    }
}