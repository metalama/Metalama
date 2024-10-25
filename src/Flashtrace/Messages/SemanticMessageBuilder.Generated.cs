// THIS FILE IS T4-GENERATED.
// To edit, go to SemanticMessageBuilder.Generated.tt.
// To transform, run RunT4.ps1.

#pragma warning disable LAMA0320 
    

#nullable enable

using System.Runtime.CompilerServices;
using Flashtrace.Messages;

namespace Flashtrace.Messages;

partial class SemanticMessageBuilder
{
    

	/// <summary>
    /// Create a semantic message with 1 parameter.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1> Semantic<T1>(  string name, string parameterName1, T1? parameterValue1 )
    {
        return new SemanticMessage<T1>( name, parameterName1, parameterValue1 );
    }

	/// <summary>
    /// Create a semantic message with 1 parameter (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1> Semantic<T1>(  string name, in (string Name,T1? Value) parameter1 )
    {
        return new SemanticMessage<T1>( name, parameter1.Name, parameter1.Value );
    }
    

	/// <summary>
    /// Create a semantic message with 2 parameters.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameterName2">Name of the second parameter.</param>
	/// <param name="parameterValue2">Name of the second parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2> Semantic<T1, T2>(  string name, string parameterName1, T1? parameterValue1, string parameterName2, T2? parameterValue2 )
    {
        return new SemanticMessage<T1, T2>( name, parameterName1, parameterValue1, parameterName2, parameterValue2 );
    }

	/// <summary>
    /// Create a semantic message with 2 parameters (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameter2">Name and value of the second parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2> Semantic<T1, T2>(  string name, in (string Name,T1? Value) parameter1, in (string Name,T2? Value) parameter2 )
    {
        return new SemanticMessage<T1, T2>( name, parameter1.Name, parameter1.Value, parameter2.Name, parameter2.Value );
    }
    

	/// <summary>
    /// Create a semantic message with 3 parameters.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameterName2">Name of the second parameter.</param>
	/// <param name="parameterValue2">Name of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameterName3">Name of the third parameter.</param>
	/// <param name="parameterValue3">Name of the third parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3> Semantic<T1, T2, T3>(  string name, string parameterName1, T1? parameterValue1, string parameterName2, T2? parameterValue2, string parameterName3, T3? parameterValue3 )
    {
        return new SemanticMessage<T1, T2, T3>( name, parameterName1, parameterValue1, parameterName2, parameterValue2, parameterName3, parameterValue3 );
    }

	/// <summary>
    /// Create a semantic message with 3 parameters (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameter2">Name and value of the second parameter wrapped as a tuple.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameter3">Name and value of the third parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3> Semantic<T1, T2, T3>(  string name, in (string Name,T1? Value) parameter1, in (string Name,T2? Value) parameter2, in (string Name,T3? Value) parameter3 )
    {
        return new SemanticMessage<T1, T2, T3>( name, parameter1.Name, parameter1.Value, parameter2.Name, parameter2.Value, parameter3.Name, parameter3.Value );
    }
    

	/// <summary>
    /// Create a semantic message with 4 parameters.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameterName2">Name of the second parameter.</param>
	/// <param name="parameterValue2">Name of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameterName3">Name of the third parameter.</param>
	/// <param name="parameterValue3">Name of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameterName4">Name of the 4-th parameter.</param>
	/// <param name="parameterValue4">Name of the 4-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4> Semantic<T1, T2, T3, T4>(  string name, string parameterName1, T1? parameterValue1, string parameterName2, T2? parameterValue2, string parameterName3, T3? parameterValue3, string parameterName4, T4? parameterValue4 )
    {
        return new SemanticMessage<T1, T2, T3, T4>( name, parameterName1, parameterValue1, parameterName2, parameterValue2, parameterName3, parameterValue3, parameterName4, parameterValue4 );
    }

	/// <summary>
    /// Create a semantic message with 4 parameters (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameter2">Name and value of the second parameter wrapped as a tuple.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameter3">Name and value of the third parameter wrapped as a tuple.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameter4">Name and value of the 4-th parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4> Semantic<T1, T2, T3, T4>(  string name, in (string Name,T1? Value) parameter1, in (string Name,T2? Value) parameter2, in (string Name,T3? Value) parameter3, in (string Name,T4? Value) parameter4 )
    {
        return new SemanticMessage<T1, T2, T3, T4>( name, parameter1.Name, parameter1.Value, parameter2.Name, parameter2.Value, parameter3.Name, parameter3.Value, parameter4.Name, parameter4.Value );
    }
    

	/// <summary>
    /// Create a semantic message with 5 parameters.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameterName2">Name of the second parameter.</param>
	/// <param name="parameterValue2">Name of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameterName3">Name of the third parameter.</param>
	/// <param name="parameterValue3">Name of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameterName4">Name of the 4-th parameter.</param>
	/// <param name="parameterValue4">Name of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameterName5">Name of the 5-th parameter.</param>
	/// <param name="parameterValue5">Name of the 5-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5> Semantic<T1, T2, T3, T4, T5>(  string name, string parameterName1, T1? parameterValue1, string parameterName2, T2? parameterValue2, string parameterName3, T3? parameterValue3, string parameterName4, T4? parameterValue4, string parameterName5, T5? parameterValue5 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5>( name, parameterName1, parameterValue1, parameterName2, parameterValue2, parameterName3, parameterValue3, parameterName4, parameterValue4, parameterName5, parameterValue5 );
    }

	/// <summary>
    /// Create a semantic message with 5 parameters (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameter2">Name and value of the second parameter wrapped as a tuple.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameter3">Name and value of the third parameter wrapped as a tuple.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameter4">Name and value of the 4-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameter5">Name and value of the 5-th parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5> Semantic<T1, T2, T3, T4, T5>(  string name, in (string Name,T1? Value) parameter1, in (string Name,T2? Value) parameter2, in (string Name,T3? Value) parameter3, in (string Name,T4? Value) parameter4, in (string Name,T5? Value) parameter5 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5>( name, parameter1.Name, parameter1.Value, parameter2.Name, parameter2.Value, parameter3.Name, parameter3.Value, parameter4.Name, parameter4.Value, parameter5.Name, parameter5.Value );
    }
    

	/// <summary>
    /// Create a semantic message with 6 parameters.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameterName2">Name of the second parameter.</param>
	/// <param name="parameterValue2">Name of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameterName3">Name of the third parameter.</param>
	/// <param name="parameterValue3">Name of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameterName4">Name of the 4-th parameter.</param>
	/// <param name="parameterValue4">Name of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameterName5">Name of the 5-th parameter.</param>
	/// <param name="parameterValue5">Name of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameterName6">Name of the 6-th parameter.</param>
	/// <param name="parameterValue6">Name of the 6-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6> Semantic<T1, T2, T3, T4, T5, T6>(  string name, string parameterName1, T1? parameterValue1, string parameterName2, T2? parameterValue2, string parameterName3, T3? parameterValue3, string parameterName4, T4? parameterValue4, string parameterName5, T5? parameterValue5, string parameterName6, T6? parameterValue6 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6>( name, parameterName1, parameterValue1, parameterName2, parameterValue2, parameterName3, parameterValue3, parameterName4, parameterValue4, parameterName5, parameterValue5, parameterName6, parameterValue6 );
    }

	/// <summary>
    /// Create a semantic message with 6 parameters (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameter2">Name and value of the second parameter wrapped as a tuple.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameter3">Name and value of the third parameter wrapped as a tuple.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameter4">Name and value of the 4-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameter5">Name and value of the 5-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameter6">Name and value of the 6-th parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6> Semantic<T1, T2, T3, T4, T5, T6>(  string name, in (string Name,T1? Value) parameter1, in (string Name,T2? Value) parameter2, in (string Name,T3? Value) parameter3, in (string Name,T4? Value) parameter4, in (string Name,T5? Value) parameter5, in (string Name,T6? Value) parameter6 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6>( name, parameter1.Name, parameter1.Value, parameter2.Name, parameter2.Value, parameter3.Name, parameter3.Value, parameter4.Name, parameter4.Value, parameter5.Name, parameter5.Value, parameter6.Name, parameter6.Value );
    }
    

	/// <summary>
    /// Create a semantic message with 7 parameters.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameterName2">Name of the second parameter.</param>
	/// <param name="parameterValue2">Name of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameterName3">Name of the third parameter.</param>
	/// <param name="parameterValue3">Name of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameterName4">Name of the 4-th parameter.</param>
	/// <param name="parameterValue4">Name of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameterName5">Name of the 5-th parameter.</param>
	/// <param name="parameterValue5">Name of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameterName6">Name of the 6-th parameter.</param>
	/// <param name="parameterValue6">Name of the 6-th parameter.</param>
	/// <typeparam name="T7">Type of the 7-th parameter value.</typeparam>
	/// <param name="parameterName7">Name of the 7-th parameter.</param>
	/// <param name="parameterValue7">Name of the 7-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6, T7> Semantic<T1, T2, T3, T4, T5, T6, T7>(  string name, string parameterName1, T1? parameterValue1, string parameterName2, T2? parameterValue2, string parameterName3, T3? parameterValue3, string parameterName4, T4? parameterValue4, string parameterName5, T5? parameterValue5, string parameterName6, T6? parameterValue6, string parameterName7, T7? parameterValue7 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6, T7>( name, parameterName1, parameterValue1, parameterName2, parameterValue2, parameterName3, parameterValue3, parameterName4, parameterValue4, parameterName5, parameterValue5, parameterName6, parameterValue6, parameterName7, parameterValue7 );
    }

	/// <summary>
    /// Create a semantic message with 7 parameters (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameter2">Name and value of the second parameter wrapped as a tuple.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameter3">Name and value of the third parameter wrapped as a tuple.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameter4">Name and value of the 4-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameter5">Name and value of the 5-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameter6">Name and value of the 6-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T7">Type of the 7-th parameter value.</typeparam>
	/// <param name="parameter7">Name and value of the 7-th parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6, T7> Semantic<T1, T2, T3, T4, T5, T6, T7>(  string name, in (string Name,T1? Value) parameter1, in (string Name,T2? Value) parameter2, in (string Name,T3? Value) parameter3, in (string Name,T4? Value) parameter4, in (string Name,T5? Value) parameter5, in (string Name,T6? Value) parameter6, in (string Name,T7? Value) parameter7 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6, T7>( name, parameter1.Name, parameter1.Value, parameter2.Name, parameter2.Value, parameter3.Name, parameter3.Value, parameter4.Name, parameter4.Value, parameter5.Name, parameter5.Value, parameter6.Name, parameter6.Value, parameter7.Name, parameter7.Value );
    }
    

	/// <summary>
    /// Create a semantic message with 8 parameters.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameterName2">Name of the second parameter.</param>
	/// <param name="parameterValue2">Name of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameterName3">Name of the third parameter.</param>
	/// <param name="parameterValue3">Name of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameterName4">Name of the 4-th parameter.</param>
	/// <param name="parameterValue4">Name of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameterName5">Name of the 5-th parameter.</param>
	/// <param name="parameterValue5">Name of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameterName6">Name of the 6-th parameter.</param>
	/// <param name="parameterValue6">Name of the 6-th parameter.</param>
	/// <typeparam name="T7">Type of the 7-th parameter value.</typeparam>
	/// <param name="parameterName7">Name of the 7-th parameter.</param>
	/// <param name="parameterValue7">Name of the 7-th parameter.</param>
	/// <typeparam name="T8">Type of the 8-th parameter value.</typeparam>
	/// <param name="parameterName8">Name of the 8-th parameter.</param>
	/// <param name="parameterValue8">Name of the 8-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8> Semantic<T1, T2, T3, T4, T5, T6, T7, T8>(  string name, string parameterName1, T1? parameterValue1, string parameterName2, T2? parameterValue2, string parameterName3, T3? parameterValue3, string parameterName4, T4? parameterValue4, string parameterName5, T5? parameterValue5, string parameterName6, T6? parameterValue6, string parameterName7, T7? parameterValue7, string parameterName8, T8? parameterValue8 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8>( name, parameterName1, parameterValue1, parameterName2, parameterValue2, parameterName3, parameterValue3, parameterName4, parameterValue4, parameterName5, parameterValue5, parameterName6, parameterValue6, parameterName7, parameterValue7, parameterName8, parameterValue8 );
    }

	/// <summary>
    /// Create a semantic message with 8 parameters (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameter2">Name and value of the second parameter wrapped as a tuple.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameter3">Name and value of the third parameter wrapped as a tuple.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameter4">Name and value of the 4-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameter5">Name and value of the 5-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameter6">Name and value of the 6-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T7">Type of the 7-th parameter value.</typeparam>
	/// <param name="parameter7">Name and value of the 7-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T8">Type of the 8-th parameter value.</typeparam>
	/// <param name="parameter8">Name and value of the 8-th parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8> Semantic<T1, T2, T3, T4, T5, T6, T7, T8>(  string name, in (string Name,T1? Value) parameter1, in (string Name,T2? Value) parameter2, in (string Name,T3? Value) parameter3, in (string Name,T4? Value) parameter4, in (string Name,T5? Value) parameter5, in (string Name,T6? Value) parameter6, in (string Name,T7? Value) parameter7, in (string Name,T8? Value) parameter8 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8>( name, parameter1.Name, parameter1.Value, parameter2.Name, parameter2.Value, parameter3.Name, parameter3.Value, parameter4.Name, parameter4.Value, parameter5.Name, parameter5.Value, parameter6.Name, parameter6.Value, parameter7.Name, parameter7.Value, parameter8.Name, parameter8.Value );
    }
    

	/// <summary>
    /// Create a semantic message with 9 parameters.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameterName2">Name of the second parameter.</param>
	/// <param name="parameterValue2">Name of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameterName3">Name of the third parameter.</param>
	/// <param name="parameterValue3">Name of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameterName4">Name of the 4-th parameter.</param>
	/// <param name="parameterValue4">Name of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameterName5">Name of the 5-th parameter.</param>
	/// <param name="parameterValue5">Name of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameterName6">Name of the 6-th parameter.</param>
	/// <param name="parameterValue6">Name of the 6-th parameter.</param>
	/// <typeparam name="T7">Type of the 7-th parameter value.</typeparam>
	/// <param name="parameterName7">Name of the 7-th parameter.</param>
	/// <param name="parameterValue7">Name of the 7-th parameter.</param>
	/// <typeparam name="T8">Type of the 8-th parameter value.</typeparam>
	/// <param name="parameterName8">Name of the 8-th parameter.</param>
	/// <param name="parameterValue8">Name of the 8-th parameter.</param>
	/// <typeparam name="T9">Type of the 9-th parameter value.</typeparam>
	/// <param name="parameterName9">Name of the 9-th parameter.</param>
	/// <param name="parameterValue9">Name of the 9-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9> Semantic<T1, T2, T3, T4, T5, T6, T7, T8, T9>(  string name, string parameterName1, T1? parameterValue1, string parameterName2, T2? parameterValue2, string parameterName3, T3? parameterValue3, string parameterName4, T4? parameterValue4, string parameterName5, T5? parameterValue5, string parameterName6, T6? parameterValue6, string parameterName7, T7? parameterValue7, string parameterName8, T8? parameterValue8, string parameterName9, T9? parameterValue9 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9>( name, parameterName1, parameterValue1, parameterName2, parameterValue2, parameterName3, parameterValue3, parameterName4, parameterValue4, parameterName5, parameterValue5, parameterName6, parameterValue6, parameterName7, parameterValue7, parameterName8, parameterValue8, parameterName9, parameterValue9 );
    }

	/// <summary>
    /// Create a semantic message with 9 parameters (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameter2">Name and value of the second parameter wrapped as a tuple.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameter3">Name and value of the third parameter wrapped as a tuple.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameter4">Name and value of the 4-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameter5">Name and value of the 5-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameter6">Name and value of the 6-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T7">Type of the 7-th parameter value.</typeparam>
	/// <param name="parameter7">Name and value of the 7-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T8">Type of the 8-th parameter value.</typeparam>
	/// <param name="parameter8">Name and value of the 8-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T9">Type of the 9-th parameter value.</typeparam>
	/// <param name="parameter9">Name and value of the 9-th parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9> Semantic<T1, T2, T3, T4, T5, T6, T7, T8, T9>(  string name, in (string Name,T1? Value) parameter1, in (string Name,T2? Value) parameter2, in (string Name,T3? Value) parameter3, in (string Name,T4? Value) parameter4, in (string Name,T5? Value) parameter5, in (string Name,T6? Value) parameter6, in (string Name,T7? Value) parameter7, in (string Name,T8? Value) parameter8, in (string Name,T9? Value) parameter9 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9>( name, parameter1.Name, parameter1.Value, parameter2.Name, parameter2.Value, parameter3.Name, parameter3.Value, parameter4.Name, parameter4.Value, parameter5.Name, parameter5.Value, parameter6.Name, parameter6.Value, parameter7.Name, parameter7.Value, parameter8.Name, parameter8.Value, parameter9.Name, parameter9.Value );
    }
    

	/// <summary>
    /// Create a semantic message with 10 parameters.
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameterName1">Name of the first parameter.</param>
	/// <param name="parameterValue1">Name of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameterName2">Name of the second parameter.</param>
	/// <param name="parameterValue2">Name of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameterName3">Name of the third parameter.</param>
	/// <param name="parameterValue3">Name of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameterName4">Name of the 4-th parameter.</param>
	/// <param name="parameterValue4">Name of the 4-th parameter.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameterName5">Name of the 5-th parameter.</param>
	/// <param name="parameterValue5">Name of the 5-th parameter.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameterName6">Name of the 6-th parameter.</param>
	/// <param name="parameterValue6">Name of the 6-th parameter.</param>
	/// <typeparam name="T7">Type of the 7-th parameter value.</typeparam>
	/// <param name="parameterName7">Name of the 7-th parameter.</param>
	/// <param name="parameterValue7">Name of the 7-th parameter.</param>
	/// <typeparam name="T8">Type of the 8-th parameter value.</typeparam>
	/// <param name="parameterName8">Name of the 8-th parameter.</param>
	/// <param name="parameterValue8">Name of the 8-th parameter.</param>
	/// <typeparam name="T9">Type of the 9-th parameter value.</typeparam>
	/// <param name="parameterName9">Name of the 9-th parameter.</param>
	/// <param name="parameterValue9">Name of the 9-th parameter.</param>
	/// <typeparam name="T10">Type of the 10-th parameter value.</typeparam>
	/// <param name="parameterName10">Name of the 10-th parameter.</param>
	/// <param name="parameterValue10">Name of the 10-th parameter.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Semantic<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(  string name, string parameterName1, T1? parameterValue1, string parameterName2, T2? parameterValue2, string parameterName3, T3? parameterValue3, string parameterName4, T4? parameterValue4, string parameterName5, T5? parameterValue5, string parameterName6, T6? parameterValue6, string parameterName7, T7? parameterValue7, string parameterName8, T8? parameterValue8, string parameterName9, T9? parameterValue9, string parameterName10, T10? parameterValue10 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>( name, parameterName1, parameterValue1, parameterName2, parameterValue2, parameterName3, parameterValue3, parameterName4, parameterValue4, parameterName5, parameterValue5, parameterName6, parameterValue6, parameterName7, parameterValue7, parameterName8, parameterValue8, parameterName9, parameterValue9, parameterName10, parameterValue10 );
    }

	/// <summary>
    /// Create a semantic message with 10 parameters (using tuples).
    /// </summary>
    /// <param name="name">Name of the message.</param>
	/// <typeparam name="T1">Type of the first parameter value.</typeparam>
	/// <param name="parameter1">Name and value of the first parameter wrapped as a tuple.</param>
	/// <typeparam name="T2">Type of the second parameter value.</typeparam>
	/// <param name="parameter2">Name and value of the second parameter wrapped as a tuple.</param>
	/// <typeparam name="T3">Type of the third parameter value.</typeparam>
	/// <param name="parameter3">Name and value of the third parameter wrapped as a tuple.</param>
	/// <typeparam name="T4">Type of the 4-th parameter value.</typeparam>
	/// <param name="parameter4">Name and value of the 4-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T5">Type of the 5-th parameter value.</typeparam>
	/// <param name="parameter5">Name and value of the 5-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T6">Type of the 6-th parameter value.</typeparam>
	/// <param name="parameter6">Name and value of the 6-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T7">Type of the 7-th parameter value.</typeparam>
	/// <param name="parameter7">Name and value of the 7-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T8">Type of the 8-th parameter value.</typeparam>
	/// <param name="parameter8">Name and value of the 8-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T9">Type of the 9-th parameter value.</typeparam>
	/// <param name="parameter9">Name and value of the 9-th parameter wrapped as a tuple.</param>
	/// <typeparam name="T10">Type of the 10-th parameter value.</typeparam>
	/// <param name="parameter10">Name and value of the 10-th parameter wrapped as a tuple.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)] // To avoid copying the struct.
    public static  SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Semantic<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(  string name, in (string Name,T1? Value) parameter1, in (string Name,T2? Value) parameter2, in (string Name,T3? Value) parameter3, in (string Name,T4? Value) parameter4, in (string Name,T5? Value) parameter5, in (string Name,T6? Value) parameter6, in (string Name,T7? Value) parameter7, in (string Name,T8? Value) parameter8, in (string Name,T9? Value) parameter9, in (string Name,T10? Value) parameter10 )
    {
        return new SemanticMessage<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>( name, parameter1.Name, parameter1.Value, parameter2.Name, parameter2.Value, parameter3.Name, parameter3.Value, parameter4.Name, parameter4.Value, parameter5.Name, parameter5.Value, parameter6.Name, parameter6.Value, parameter7.Name, parameter7.Value, parameter8.Name, parameter8.Value, parameter9.Name, parameter9.Value, parameter10.Name, parameter10.Value );
    }
    
}