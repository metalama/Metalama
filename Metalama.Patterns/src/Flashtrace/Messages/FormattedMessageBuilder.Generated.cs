// THIS FILE IS T4-GENERATED.
// To edit, go to FormattedMessageBuilder.Generated.tt.
// To transform, run RunT4.ps1.

#pragma warning disable LAMA0320 
    

#nullable enable

using System.Runtime.CompilerServices;
using Flashtrace.Messages;

namespace Flashtrace.Messages;

partial class FormattedMessageBuilder
{

	/// <summary>
    /// Creates a formatted string with 1 parameter.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1> Formatted<T1>(  string formattingString, T1? arg1 )
    {
        return new FormattedMessage<T1>( formattingString, arg1 );
    }

	/// <summary>
    /// Creates a formatted string with 2 parameters.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1, T2> Formatted<T1, T2>(  string formattingString, T1? arg1, T2? arg2 )
    {
        return new FormattedMessage<T1, T2>( formattingString, arg1, arg2 );
    }

	/// <summary>
    /// Creates a formatted string with 3 parameters.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1, T2, T3> Formatted<T1, T2, T3>(  string formattingString, T1? arg1, T2? arg2, T3? arg3 )
    {
        return new FormattedMessage<T1, T2, T3>( formattingString, arg1, arg2, arg3 );
    }

	/// <summary>
    /// Creates a formatted string with 4 parameters.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter.</typeparam>
	/// <param name="arg4">Value of the 4-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1, T2, T3, T4> Formatted<T1, T2, T3, T4>(  string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4 )
    {
        return new FormattedMessage<T1, T2, T3, T4>( formattingString, arg1, arg2, arg3, arg4 );
    }

	/// <summary>
    /// Creates a formatted string with 5 parameters.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter.</typeparam>
	/// <param name="arg4">Value of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter.</typeparam>
	/// <param name="arg5">Value of the 5-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1, T2, T3, T4, T5> Formatted<T1, T2, T3, T4, T5>(  string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5 )
    {
        return new FormattedMessage<T1, T2, T3, T4, T5>( formattingString, arg1, arg2, arg3, arg4, arg5 );
    }

	/// <summary>
    /// Creates a formatted string with 6 parameters.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter.</typeparam>
	/// <param name="arg4">Value of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter.</typeparam>
	/// <param name="arg5">Value of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter.</typeparam>
	/// <param name="arg6">Value of the 6-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1, T2, T3, T4, T5, T6> Formatted<T1, T2, T3, T4, T5, T6>(  string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6 )
    {
        return new FormattedMessage<T1, T2, T3, T4, T5, T6>( formattingString, arg1, arg2, arg3, arg4, arg5, arg6 );
    }

	/// <summary>
    /// Creates a formatted string with 7 parameters.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter.</typeparam>
	/// <param name="arg4">Value of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter.</typeparam>
	/// <param name="arg5">Value of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter.</typeparam>
	/// <param name="arg6">Value of the 6-th parameter.</param>
	/// <typeparam name="T7">Type of the 7-th parameter.</typeparam>
	/// <param name="arg7">Value of the 7-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1, T2, T3, T4, T5, T6, T7> Formatted<T1, T2, T3, T4, T5, T6, T7>(  string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7 )
    {
        return new FormattedMessage<T1, T2, T3, T4, T5, T6, T7>( formattingString, arg1, arg2, arg3, arg4, arg5, arg6, arg7 );
    }

	/// <summary>
    /// Creates a formatted string with 8 parameters.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter.</typeparam>
	/// <param name="arg4">Value of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter.</typeparam>
	/// <param name="arg5">Value of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter.</typeparam>
	/// <param name="arg6">Value of the 6-th parameter.</param>
	/// <typeparam name="T7">Type of the 7-th parameter.</typeparam>
	/// <param name="arg7">Value of the 7-th parameter.</param>
	/// <typeparam name="T8">Type of the 8-th parameter.</typeparam>
	/// <param name="arg8">Value of the 8-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1, T2, T3, T4, T5, T6, T7, T8> Formatted<T1, T2, T3, T4, T5, T6, T7, T8>(  string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8 )
    {
        return new FormattedMessage<T1, T2, T3, T4, T5, T6, T7, T8>( formattingString, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8 );
    }

	/// <summary>
    /// Creates a formatted string with 9 parameters.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter.</typeparam>
	/// <param name="arg4">Value of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter.</typeparam>
	/// <param name="arg5">Value of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter.</typeparam>
	/// <param name="arg6">Value of the 6-th parameter.</param>
	/// <typeparam name="T7">Type of the 7-th parameter.</typeparam>
	/// <param name="arg7">Value of the 7-th parameter.</param>
	/// <typeparam name="T8">Type of the 8-th parameter.</typeparam>
	/// <param name="arg8">Value of the 8-th parameter.</param>
	/// <typeparam name="T9">Type of the 9-th parameter.</typeparam>
	/// <param name="arg9">Value of the 9-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9> Formatted<T1, T2, T3, T4, T5, T6, T7, T8, T9>(  string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9 )
    {
        return new FormattedMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9>( formattingString, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9 );
    }

	/// <summary>
    /// Creates a formatted string with 10 parameters.
    /// </summary>
    /// <param name="formattingString">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
	/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter.</typeparam>
	/// <param name="arg4">Value of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter.</typeparam>
	/// <param name="arg5">Value of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter.</typeparam>
	/// <param name="arg6">Value of the 6-th parameter.</param>
	/// <typeparam name="T7">Type of the 7-th parameter.</typeparam>
	/// <param name="arg7">Value of the 7-th parameter.</param>
	/// <typeparam name="T8">Type of the 8-th parameter.</typeparam>
	/// <param name="arg8">Value of the 8-th parameter.</param>
	/// <typeparam name="T9">Type of the 9-th parameter.</typeparam>
	/// <param name="arg9">Value of the 9-th parameter.</param>
	/// <typeparam name="T10">Type of the 10-th parameter.</typeparam>
	/// <param name="arg10">Value of the 10-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static FormattedMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Formatted<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(  string formattingString, T1? arg1, T2? arg2, T3? arg3, T4? arg4, T5? arg5, T6? arg6, T7? arg7, T8? arg8, T9? arg9, T10? arg10 )
    {
        return new FormattedMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>( formattingString, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10 );
    }
}