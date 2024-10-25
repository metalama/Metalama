// THIS FILE IS T4-GENERATED.
// To edit, go to FormattedMessage.Generated.tt.
// To transform, run RunT4.ps1.

#pragma warning disable LAMA0320 
    

#nullable enable

using Flashtrace.Records;
using System.Runtime.CompilerServices;

namespace Flashtrace.Messages;

/// <summary>
/// Encapsulates a text message with 1 parameter. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(1, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 1 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 1 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

/// <summary>
/// Encapsulates a text message with 2 parameters. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1, T2> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; private readonly T2? arg2; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1, T2? arg2 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; this.arg2 = arg2; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(2, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 2 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 2 parameters.");
		}

		recordBuilder.WriteParameter( 1, parameter, arg2, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 2 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

/// <summary>
/// Encapsulates a text message with 3 parameters. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1, T2, T3> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; private readonly T2? arg2; private readonly T3? arg3; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1, T2? arg2, T3? arg3 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(3, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 3 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 3 parameters.");
		}

		recordBuilder.WriteParameter( 1, parameter, arg2, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 3 parameters.");
		}

		recordBuilder.WriteParameter( 2, parameter, arg3, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 3 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

/// <summary>
/// Encapsulates a text message with 4 parameters. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1, T2, T3, T4> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; private readonly T2? arg2; private readonly T3? arg3; private readonly T4? arg4; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(4, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 4 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 4 parameters.");
		}

		recordBuilder.WriteParameter( 1, parameter, arg2, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 4 parameters.");
		}

		recordBuilder.WriteParameter( 2, parameter, arg3, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 4 parameters.");
		}

		recordBuilder.WriteParameter( 3, parameter, arg4, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 4 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

/// <summary>
/// Encapsulates a text message with 5 parameters. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1, T2, T3, T4, T5> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; private readonly T2? arg2; private readonly T3? arg3; private readonly T4? arg4; private readonly T5? arg5; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(5, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 5 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 5 parameters.");
		}

		recordBuilder.WriteParameter( 1, parameter, arg2, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 5 parameters.");
		}

		recordBuilder.WriteParameter( 2, parameter, arg3, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 5 parameters.");
		}

		recordBuilder.WriteParameter( 3, parameter, arg4, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 5 parameters.");
		}

		recordBuilder.WriteParameter( 4, parameter, arg5, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 5 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

/// <summary>
/// Encapsulates a text message with 6 parameters. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1, T2, T3, T4, T5, T6> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; private readonly T2? arg2; private readonly T3? arg3; private readonly T4? arg4; private readonly T5? arg5; private readonly T6? arg6; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(6, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 6 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 6 parameters.");
		}

		recordBuilder.WriteParameter( 1, parameter, arg2, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 6 parameters.");
		}

		recordBuilder.WriteParameter( 2, parameter, arg3, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 6 parameters.");
		}

		recordBuilder.WriteParameter( 3, parameter, arg4, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 6 parameters.");
		}

		recordBuilder.WriteParameter( 4, parameter, arg5, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 6 parameters.");
		}

		recordBuilder.WriteParameter( 5, parameter, arg6, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 6 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

/// <summary>
/// Encapsulates a text message with 7 parameters. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1, T2, T3, T4, T5, T6, T7> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; private readonly T2? arg2; private readonly T3? arg3; private readonly T4? arg4; private readonly T5? arg5; private readonly T6? arg6; private readonly T7? arg7; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(7, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 7 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 7 parameters.");
		}

		recordBuilder.WriteParameter( 1, parameter, arg2, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 7 parameters.");
		}

		recordBuilder.WriteParameter( 2, parameter, arg3, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 7 parameters.");
		}

		recordBuilder.WriteParameter( 3, parameter, arg4, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 7 parameters.");
		}

		recordBuilder.WriteParameter( 4, parameter, arg5, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 7 parameters.");
		}

		recordBuilder.WriteParameter( 5, parameter, arg6, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 7 parameters.");
		}

		recordBuilder.WriteParameter( 6, parameter, arg7, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 7 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

/// <summary>
/// Encapsulates a text message with 8 parameters. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1, T2, T3, T4, T5, T6, T7, T8> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; private readonly T2? arg2; private readonly T3? arg3; private readonly T4? arg4; private readonly T5? arg5; private readonly T6? arg6; private readonly T7? arg7; private readonly T8? arg8; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; this.arg8 = arg8; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(8, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 8 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 8 parameters.");
		}

		recordBuilder.WriteParameter( 1, parameter, arg2, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 8 parameters.");
		}

		recordBuilder.WriteParameter( 2, parameter, arg3, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 8 parameters.");
		}

		recordBuilder.WriteParameter( 3, parameter, arg4, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 8 parameters.");
		}

		recordBuilder.WriteParameter( 4, parameter, arg5, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 8 parameters.");
		}

		recordBuilder.WriteParameter( 5, parameter, arg6, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 8 parameters.");
		}

		recordBuilder.WriteParameter( 6, parameter, arg7, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 8 parameters.");
		}

		recordBuilder.WriteParameter( 7, parameter, arg8, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 8 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

/// <summary>
/// Encapsulates a text message with 9 parameters. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; private readonly T2? arg2; private readonly T3? arg3; private readonly T4? arg4; private readonly T5? arg5; private readonly T6? arg6; private readonly T7? arg7; private readonly T8? arg8; private readonly T9? arg9; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; this.arg8 = arg8; this.arg9 = arg9; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(9, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.WriteParameter( 1, parameter, arg2, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.WriteParameter( 2, parameter, arg3, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.WriteParameter( 3, parameter, arg4, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.WriteParameter( 4, parameter, arg5, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.WriteParameter( 5, parameter, arg6, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.WriteParameter( 6, parameter, arg7, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.WriteParameter( 7, parameter, arg8, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.WriteParameter( 8, parameter, arg9, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 9 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

/// <summary>
/// Encapsulates a text message with 10 parameters. Use the <see cref="FormattedMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct FormattedMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IMessage
{
	private readonly string formattingString;
	
	private readonly T1? arg1; private readonly T2? arg2; private readonly T3? arg3; private readonly T4? arg4; private readonly T5? arg5; private readonly T6? arg6; private readonly T7? arg7; private readonly T8? arg8; private readonly T9? arg9; private readonly T10? arg10; 

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal FormattedMessage( string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9, T10? arg10 )
	{
		this.formattingString = formattingString;
		this.arg1 = arg1; this.arg2 = arg2; this.arg3 = arg3; this.arg4 = arg4; this.arg5 = arg5; this.arg6 = arg6; this.arg7 = arg7; this.arg8 = arg8; this.arg9 = arg9; this.arg10 = arg10; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(10, null));

		FormattingStringParser parser = new FormattingStringParser( this.formattingString.AsSpan() );
		ReadOnlySpan<char> parameter;

		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 0, parameter, arg1, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 1, parameter, arg2, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 2, parameter, arg3, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 3, parameter, arg4, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 4, parameter, arg5, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 5, parameter, arg6, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 6, parameter, arg7, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 7, parameter, arg8, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 8, parameter, arg9, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );


		if ( !parser.TryGetNextParameter( out parameter ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.WriteParameter( 9, parameter, arg10, LogParameterOptions.FormattedStringParameter );
		recordBuilder.WriteString( parser.GetNextText() );

		if ( parser.TryGetNextParameter( out _ ) )
		{
			throw new InvalidFormattingStringException("The formatting string must have exactly 10 parameters.");
		}

		recordBuilder.EndWriteItem(item);


	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}
