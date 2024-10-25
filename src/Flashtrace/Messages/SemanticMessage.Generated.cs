// THIS FILE IS T4-GENERATED.
// To edit, go to SemanticMessage.Generated.tt.
// To transform, run RunT4.ps1.

#pragma warning disable LAMA0320 
    

#nullable enable

using Flashtrace.Records;
using System.Runtime.CompilerServices;

namespace Flashtrace.Messages;

 /// <summary>
/// Encapsulates a semantic message with a 1 number of parameter. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; 
	private readonly T1? value1; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1 )
	{
		this.messageName = messageName;
		this.name1 = name1; 
		this.value1 = value1; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(1, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

 /// <summary>
/// Encapsulates a semantic message with a 2 number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1, T2> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; private readonly string name2; 
	private readonly T1? value1; private readonly T2? value2; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1, string name2, T2? value2 )
	{
		this.messageName = messageName;
		this.name1 = name1; this.name2 = name2; 
		this.value1 = value1; this.value2 = value2; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(2, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 1, this.name2.AsSpan(), this.value2, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

 /// <summary>
/// Encapsulates a semantic message with a 3 number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1, T2, T3> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; private readonly string name2; private readonly string name3; 
	private readonly T1? value1; private readonly T2? value2; private readonly T3? value3; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1, string name2, T2? value2, string name3, T3? value3 )
	{
		this.messageName = messageName;
		this.name1 = name1; this.name2 = name2; this.name3 = name3; 
		this.value1 = value1; this.value2 = value2; this.value3 = value3; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(3, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 1, this.name2.AsSpan(), this.value2, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 2, this.name3.AsSpan(), this.value3, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

 /// <summary>
/// Encapsulates a semantic message with a 4 number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1, T2, T3, T4> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; private readonly string name2; private readonly string name3; private readonly string name4; 
	private readonly T1? value1; private readonly T2? value2; private readonly T3? value3; private readonly T4? value4; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1, string name2, T2? value2, string name3, T3? value3, string name4, T4? value4 )
	{
		this.messageName = messageName;
		this.name1 = name1; this.name2 = name2; this.name3 = name3; this.name4 = name4; 
		this.value1 = value1; this.value2 = value2; this.value3 = value3; this.value4 = value4; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(4, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 1, this.name2.AsSpan(), this.value2, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 2, this.name3.AsSpan(), this.value3, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 3, this.name4.AsSpan(), this.value4, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

 /// <summary>
/// Encapsulates a semantic message with a 5 number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1, T2, T3, T4, T5> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; private readonly string name2; private readonly string name3; private readonly string name4; private readonly string name5; 
	private readonly T1? value1; private readonly T2? value2; private readonly T3? value3; private readonly T4? value4; private readonly T5? value5; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1, string name2, T2? value2, string name3, T3? value3, string name4, T4? value4, string name5, T5? value5 )
	{
		this.messageName = messageName;
		this.name1 = name1; this.name2 = name2; this.name3 = name3; this.name4 = name4; this.name5 = name5; 
		this.value1 = value1; this.value2 = value2; this.value3 = value3; this.value4 = value4; this.value5 = value5; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(5, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 1, this.name2.AsSpan(), this.value2, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 2, this.name3.AsSpan(), this.value3, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 3, this.name4.AsSpan(), this.value4, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 4, this.name5.AsSpan(), this.value5, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

 /// <summary>
/// Encapsulates a semantic message with a 6 number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1, T2, T3, T4, T5, T6> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; private readonly string name2; private readonly string name3; private readonly string name4; private readonly string name5; private readonly string name6; 
	private readonly T1? value1; private readonly T2? value2; private readonly T3? value3; private readonly T4? value4; private readonly T5? value5; private readonly T6? value6; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1, string name2, T2? value2, string name3, T3? value3, string name4, T4? value4, string name5, T5? value5, string name6, T6? value6 )
	{
		this.messageName = messageName;
		this.name1 = name1; this.name2 = name2; this.name3 = name3; this.name4 = name4; this.name5 = name5; this.name6 = name6; 
		this.value1 = value1; this.value2 = value2; this.value3 = value3; this.value4 = value4; this.value5 = value5; this.value6 = value6; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(6, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 1, this.name2.AsSpan(), this.value2, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 2, this.name3.AsSpan(), this.value3, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 3, this.name4.AsSpan(), this.value4, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 4, this.name5.AsSpan(), this.value5, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 5, this.name6.AsSpan(), this.value6, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

 /// <summary>
/// Encapsulates a semantic message with a 7 number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1, T2, T3, T4, T5, T6, T7> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; private readonly string name2; private readonly string name3; private readonly string name4; private readonly string name5; private readonly string name6; private readonly string name7; 
	private readonly T1? value1; private readonly T2? value2; private readonly T3? value3; private readonly T4? value4; private readonly T5? value5; private readonly T6? value6; private readonly T7? value7; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1, string name2, T2? value2, string name3, T3? value3, string name4, T4? value4, string name5, T5? value5, string name6, T6? value6, string name7, T7? value7 )
	{
		this.messageName = messageName;
		this.name1 = name1; this.name2 = name2; this.name3 = name3; this.name4 = name4; this.name5 = name5; this.name6 = name6; this.name7 = name7; 
		this.value1 = value1; this.value2 = value2; this.value3 = value3; this.value4 = value4; this.value5 = value5; this.value6 = value6; this.value7 = value7; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(7, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 1, this.name2.AsSpan(), this.value2, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 2, this.name3.AsSpan(), this.value3, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 3, this.name4.AsSpan(), this.value4, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 4, this.name5.AsSpan(), this.value5, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 5, this.name6.AsSpan(), this.value6, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 6, this.name7.AsSpan(), this.value7, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

 /// <summary>
/// Encapsulates a semantic message with a 8 number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; private readonly string name2; private readonly string name3; private readonly string name4; private readonly string name5; private readonly string name6; private readonly string name7; private readonly string name8; 
	private readonly T1? value1; private readonly T2? value2; private readonly T3? value3; private readonly T4? value4; private readonly T5? value5; private readonly T6? value6; private readonly T7? value7; private readonly T8? value8; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1, string name2, T2? value2, string name3, T3? value3, string name4, T4? value4, string name5, T5? value5, string name6, T6? value6, string name7, T7? value7, string name8, T8? value8 )
	{
		this.messageName = messageName;
		this.name1 = name1; this.name2 = name2; this.name3 = name3; this.name4 = name4; this.name5 = name5; this.name6 = name6; this.name7 = name7; this.name8 = name8; 
		this.value1 = value1; this.value2 = value2; this.value3 = value3; this.value4 = value4; this.value5 = value5; this.value6 = value6; this.value7 = value7; this.value8 = value8; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(8, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 1, this.name2.AsSpan(), this.value2, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 2, this.name3.AsSpan(), this.value3, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 3, this.name4.AsSpan(), this.value4, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 4, this.name5.AsSpan(), this.value5, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 5, this.name6.AsSpan(), this.value6, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 6, this.name7.AsSpan(), this.value7, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 7, this.name8.AsSpan(), this.value8, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

 /// <summary>
/// Encapsulates a semantic message with a 9 number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; private readonly string name2; private readonly string name3; private readonly string name4; private readonly string name5; private readonly string name6; private readonly string name7; private readonly string name8; private readonly string name9; 
	private readonly T1? value1; private readonly T2? value2; private readonly T3? value3; private readonly T4? value4; private readonly T5? value5; private readonly T6? value6; private readonly T7? value7; private readonly T8? value8; private readonly T9? value9; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1, string name2, T2? value2, string name3, T3? value3, string name4, T4? value4, string name5, T5? value5, string name6, T6? value6, string name7, T7? value7, string name8, T8? value8, string name9, T9? value9 )
	{
		this.messageName = messageName;
		this.name1 = name1; this.name2 = name2; this.name3 = name3; this.name4 = name4; this.name5 = name5; this.name6 = name6; this.name7 = name7; this.name8 = name8; this.name9 = name9; 
		this.value1 = value1; this.value2 = value2; this.value3 = value3; this.value4 = value4; this.value5 = value5; this.value6 = value6; this.value7 = value7; this.value8 = value8; this.value9 = value9; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(9, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 1, this.name2.AsSpan(), this.value2, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 2, this.name3.AsSpan(), this.value3, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 3, this.name4.AsSpan(), this.value4, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 4, this.name5.AsSpan(), this.value5, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 5, this.name6.AsSpan(), this.value6, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 6, this.name7.AsSpan(), this.value7, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 7, this.name8.AsSpan(), this.value8, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 8, this.name9.AsSpan(), this.value9, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}

 /// <summary>
/// Encapsulates a semantic message with a 10 number of parameters. Use the <see cref="SemanticMessageBuilder"/> class to create an instance of this type.
/// </summary>
public readonly struct SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IMessage
{
	private readonly string messageName;
	
	private readonly string name1; private readonly string name2; private readonly string name3; private readonly string name4; private readonly string name5; private readonly string name6; private readonly string name7; private readonly string name8; private readonly string name9; private readonly string name10; 
	private readonly T1? value1; private readonly T2? value2; private readonly T3? value3; private readonly T4? value4; private readonly T5? value5; private readonly T6? value6; private readonly T7? value7; private readonly T8? value8; private readonly T9? value9; private readonly T10? value10; 

	[MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
	internal SemanticMessage( string messageName, string name1, T1? value1, string name2, T2? value2, string name3, T3? value3, string name4, T4? value4, string name5, T5? value5, string name6, T6? value6, string name7, T7? value7, string name8, T8? value8, string name9, T9? value9, string name10, T10? value10 )
	{
		this.messageName = messageName;
		this.name1 = name1; this.name2 = name2; this.name3 = name3; this.name4 = name4; this.name5 = name5; this.name6 = name6; this.name7 = name7; this.name8 = name8; this.name9 = name9; this.name10 = name10; 
		this.value1 = value1; this.value2 = value2; this.value3 = value3; this.value4 = value4; this.value5 = value5; this.value6 = value6; this.value7 = value7; this.value8 = value8; this.value9 = value9; this.value10 = value10; 
	}

	void IMessage.Write( ILogRecordBuilder recordBuilder, LogRecordItem item )
	{
		recordBuilder.BeginWriteItem(item, new LogRecordTextOptions(10, this.messageName));
		recordBuilder.WriteParameter( 0, this.name1.AsSpan(), this.value1, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 1, this.name2.AsSpan(), this.value2, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 2, this.name3.AsSpan(), this.value3, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 3, this.name4.AsSpan(), this.value4, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 4, this.name5.AsSpan(), this.value5, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 5, this.name6.AsSpan(), this.value6, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 6, this.name7.AsSpan(), this.value7, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 7, this.name8.AsSpan(), this.value8, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 8, this.name9.AsSpan(), this.value9, LogParameterOptions.SemanticParameter );
		recordBuilder.WriteParameter( 9, this.name10.AsSpan(), this.value10, LogParameterOptions.SemanticParameter );
		recordBuilder.EndWriteItem(item);
		
	}

	/// <inheritdoc />
	public override string ToString() => DebugMessageFormatter.Format( this );
}
