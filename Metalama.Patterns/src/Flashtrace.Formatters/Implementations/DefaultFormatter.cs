// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Flashtrace.Formatters.Implementations;

// Must be a separate class because we want to share among different generic types.

/// <summary>
/// The default formatter that formats objects by calling <see cref="object.ToString"/>.
/// </summary>
internal sealed class DefaultFormatter<TValue> : Formatter<TValue>
{
    private static readonly bool _isValueType = typeof(TValue).IsValueType;

    private static readonly bool _hasCustomToStringMethod = DefaultFormatterHelper.HasCustomToStringMethod( typeof(TValue) );

    private static readonly Type _valueType = typeof(TValue);

    private readonly FormattingOptions _options;

    private readonly DefaultFormatter<TValue> _otherQuotingFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultFormatter{TValue}"/> class.
    /// </summary>
    public DefaultFormatter( IFormatterRepository repository )
        : base( repository )
    {
        this._options = FormattingOptions.Default;
        this._otherQuotingFormatter = new DefaultFormatter<TValue>( repository, FormattingOptions.Unquoted, this );
    }

    private DefaultFormatter( IFormatterRepository repository, FormattingOptions options, DefaultFormatter<TValue> otherQuotingFormatter )
        : base( repository )
    {
        this._options = options;
        this._otherQuotingFormatter = otherQuotingFormatter;
    }

    /// <inheritdoc/>
    public override FormatterAttributes Attributes => FormatterAttributes.Default;

    /// <inheritdoc />
    public override IOptionAwareFormatter WithOptions( FormattingOptions options )
    {
        if ( options.RequiresUnquotedStrings )
        {
            return this._options == FormattingOptions.Unquoted ? this : this._otherQuotingFormatter;
        }
        else
        {
            return this._options == FormattingOptions.Default ? this : this._otherQuotingFormatter;
        }
    }

    /// <inheritdoc />
    public override void Format( UnsafeStringBuilder stringBuilder, TValue? value )
    {
        var useToString = _hasCustomToStringMethod;
        var thisValueType = _valueType;

        if ( _isValueType )
        {
            var formatter = this.Repository.Get<TValue>().WithOptions( this._options );

            if ( (formatter.Attributes & FormatterAttributes.Default) == 0 )
            {
                formatter.Format( stringBuilder, value );

                return;
            }
        }
        else
        {
            // ReSharper disable HeapView.PossibleBoxingAllocation
            // ReSharper disable once CompareNonConstrainedGenericWithNull

            if ( value == null )
            {
                stringBuilder.Append( 'n', 'u', 'l', 'l' );

                return;
            }

            var formatter = this.Repository.Get( value.GetType() ).WithOptions( this._options );

            if ( (formatter.Attributes & FormatterAttributes.Default) == 0 )
            {
                formatter.Format( stringBuilder, value );

                return;
            }

            thisValueType = value.GetType();

            if ( thisValueType != typeof(TValue) )
            {
                // We get here because the caller called the static Get<T> and we have a value of the derived type,
                // not the exact type. 
                useToString = DefaultFormatterHelper.HasCustomToStringMethod( thisValueType );
            }

            // ReSharper restore HeapView.PossibleBoxingAllocation
        }

        if ( useToString )
        {
            string? text;

            try
            {
                text = value!.ToString();
            }
            catch ( Exception e )
            {
                text = e.GetType().ToString();
            }

            var braces = !value!.GetType().IsPrimitive;

            if ( braces )
            {
                stringBuilder.Append( '{' );
            }

            // ToString can return null.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if ( text == null )
            {
                stringBuilder.Append( 'n', 'u', 'l', 'l' );
            }
            else
            {
                stringBuilder.Append( text );
            }

            if ( braces )
            {
                stringBuilder.Append( '}' );
            }
        }
        else
        {
            stringBuilder.Append( '{' );
            this.Repository.Get<Type>().Format( stringBuilder, thisValueType );
            stringBuilder.Append( '}' );
        }
    }
}