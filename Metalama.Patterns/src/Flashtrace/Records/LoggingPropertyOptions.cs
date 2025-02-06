// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;
using JetBrains.Annotations;

namespace Flashtrace.Records;

/// <summary>
/// Specifies the behavior of logging properties (exposed by <see cref="LogEventData"/>), such as
/// <see cref="IsRendered"/>, <see cref="IsInherited"/> or <see cref="IsBaggage"/>.
/// </summary>
[PublicAPI]
public readonly struct LoggingPropertyOptions
{
    private readonly Flags _flags;

    private LoggingPropertyOptions( Flags flags, IFormatter? formatter )
    {
        this._flags = flags;
        this.Formatter = formatter;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingPropertyOptions"/> struct.
    /// Initializes a new <see cref="LoggingPropertyOptions"/>.
    /// </summary>
    /// <param name="isRendered">Determines whether the property will be included in the log message. The default value is <c>false</c>, then
    /// the property is only available as an additional property, if this is supported by the backend.</param>
    /// <param name="isInherited"> Determines whether the property is inherited from the parent activity to children activities and messages. The default value is <c>true</c>.
    ///  When this property is set to <c>false</c>, <see cref="IsBaggage"/> is automatically set to <c>false</c>.
    /// </param>
    /// <param name="isBaggage">
    ///  Determines whether the property is cross-process. The default value is <c>false</c>. When this property is set to <c>true</c>, <see cref="IsInherited"/> is automatically
    /// set to <c>true</c>.
    /// </param>
    /// <param name="isIgnored">Determines whether this property must be ignored by the <see cref="LogEventMetadata.VisitProperties{TVisitorState}"/>
    /// method.</param>
    /// <param name="formatter">The formatter to be used to render the property value.</param>
    public LoggingPropertyOptions(
        bool isRendered = false,
        bool isInherited = false,
        bool isBaggage = false,
        bool isIgnored = false,
        IFormatter? formatter = null )
    {
        this._flags = Flags.None;

        if ( isRendered )
        {
            this._flags |= Flags.IsRendered;
        }

        if ( isInherited )
        {
            this._flags |= Flags.IsInherited;
        }

        if ( isBaggage )
        {
            this._flags |= Flags.IsBaggage | Flags.IsInherited;
        }

        if ( isIgnored )
        {
            this._flags |= Flags.IsIgnored;
        }

        this.Formatter = formatter;
    }

    /// <summary>
    /// Gets the formatter to be used to render the property value.
    /// </summary>
    public IFormatter? Formatter { get; }

    /// <summary>
    /// Returns a copy of the current <seealso cref="LoggingPropertyOptions"/> but with a different value of the <see cref="Formatter"/> property.
    /// </summary>
    /// <param name="formatter">The formatter to be used to render the property value.</param>
    /// <returns></returns>
    public LoggingPropertyOptions WithFormatter( IFormatter formatter )
    {
        return new LoggingPropertyOptions( this._flags, formatter );
    }

    /// <summary>
    /// Gets a value indicating whether this property must be ignored by the <see cref="LogEventMetadata.VisitProperties{TVisitorState}"/>
    /// method. This value is typically only returned by <see cref="LogEventMetadata.GetPropertyOptions(string)"/> to say that a property of the raw CLR object
    /// must not be exposed as a logging property.
    /// </summary>
    public bool IsIgnored => (this._flags & Flags.IsIgnored) != 0;

    /// <summary>
    /// Returns a copy of the current <seealso cref="LoggingPropertyOptions"/> but with a different value of the <see cref="IsIgnored"/> property.
    /// </summary>
    /// <param name="value">New value of the <see cref="IsIgnored"/> property.</param>
    /// <returns></returns>
    public LoggingPropertyOptions WithIsIgnored( bool value )
    {
        var otherFlags = this._flags & ~Flags.IsIgnored;
        var thisFlag = value ? Flags.IsIgnored : Flags.None;

        return new LoggingPropertyOptions( otherFlags | thisFlag, this.Formatter );
    }

    /// <summary>
    /// Gets a value indicating whether the property will be included in the log message. The default value is <c>false</c>, then
    /// the property is only available as an additional property, if this is supported by the backend.
    /// </summary>
    public bool IsRendered => (this._flags & Flags.IsRendered) != 0;

    /// <summary>
    /// Returns a copy of the current <seealso cref="LoggingPropertyOptions"/> but with a different value of the <see cref="IsRendered"/> property.
    /// </summary>
    /// <param name="value">New value of the <see cref="IsRendered"/> property.</param>
    /// <returns></returns>
    public LoggingPropertyOptions WithIsRendered( bool value )
    {
        var otherFlags = this._flags & ~Flags.IsRendered;
        var thisFlag = value ? Flags.IsRendered : Flags.None;

        return new LoggingPropertyOptions( otherFlags | thisFlag, this.Formatter );
    }

    /// <summary>
    /// Gets a value indicating whether the property is inherited from the parent activity to children activities and messages. The default value is <c>true</c>.
    ///  When this property is set to <c>false</c>, <see cref="IsBaggage"/> is automatically set to <c>false</c>.
    /// </summary>
    public bool IsInherited => (this._flags & Flags.IsInherited) != 0;

    /// <summary>
    /// Returns a copy of the current <seealso cref="LoggingPropertyOptions"/> but with a different value of the <see cref="IsInherited"/> property.
    /// </summary>
    /// <param name="value">New value of the <see cref="IsInherited"/> property.</param>
    /// <returns></returns>
    public LoggingPropertyOptions WithIsInherited( bool value )
    {
        var otherFlags = this._flags & ~Flags.IsInherited;
        var thisFlag = value ? Flags.IsInherited : Flags.None;

        return new LoggingPropertyOptions( otherFlags | thisFlag, this.Formatter );
    }

    /// <summary>
    /// Gets a value indicating whether the property is cross-process. The default value is <c>false</c>. When this property is set to <c>true</c>, <see cref="IsInherited"/> is automatically
    /// set to <c>true</c>.
    /// </summary>
    public bool IsBaggage => (this._flags & Flags.IsBaggage) != 0;

    /// <summary>
    /// Returns a copy of the current <seealso cref="LoggingPropertyOptions"/> but with a different value of the <see cref="IsBaggage"/> property.
    /// </summary>
    /// <param name="value">New value of the <see cref="IsBaggage"/> property.</param>
    /// <returns></returns>
    public LoggingPropertyOptions WithIsBaggage( bool value )
    {
        var otherFlags = this._flags & ~Flags.IsBaggage;
        var thisFlag = value ? Flags.IsBaggage | Flags.IsInherited : Flags.None;

        return new LoggingPropertyOptions( otherFlags | thisFlag, this.Formatter );
    }

    [Flags]
    private enum Flags
    {
        None,
        IsRendered = 1,
        IsInherited = 2,
        IsBaggage = 4,
        IsIgnored = 8
    }
}