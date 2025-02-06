// THIS FILE IS T4-GENERATED.
// To edit, go to ILogger.Generated.tt.
// To transform, run RunT4.ps1.

#pragma warning disable LAMA0320 
    

using Flashtrace.Contexts;
using Flashtrace.Records;
using Microsoft.Extensions.Logging;

namespace Flashtrace
{
	public partial interface IFlashtraceLogger
	{
	
		/// <summary>
        /// Writes a log record with 1 parameter.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
			/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	   void Write<T1>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, Exception exception, in CallerInfo recordInfo  );


	
		/// <summary>
        /// Writes a log record with 2 parameters.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
			/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	   void Write<T1, T2>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, T2 arg2, Exception exception, in CallerInfo recordInfo  );


	
		/// <summary>
        /// Writes a log record with 3 parameters.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
			/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
	   void Write<T1, T2, T3>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, T2 arg2, T3 arg3, Exception exception, in CallerInfo recordInfo  );


	
		/// <summary>
        /// Writes a log record with 4 parameters.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
			/// <typeparam name="T1">Type of the first parameter.</typeparam>
	/// <param name="arg1">Value of the first parameter.</param>
	/// <typeparam name="T2">Type of the second parameter.</typeparam>
	/// <param name="arg2">Value of the second parameter.</param>
	/// <typeparam name="T3">Type of the third parameter.</typeparam>
	/// <param name="arg3">Value of the third parameter.</param>
	/// <typeparam name="T4">Type of the 4-th parameter.</typeparam>
	/// <param name="arg4">Value of the 4-th parameter.</param>
	   void Write<T1, T2, T3, T4>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, T2 arg2, T3 arg3, T4 arg4, Exception exception, in CallerInfo recordInfo  );


	
		/// <summary>
        /// Writes a log record with 5 parameters.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
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
	   void Write<T1, T2, T3, T4, T5>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, Exception exception, in CallerInfo recordInfo  );


	
		/// <summary>
        /// Writes a log record with 6 parameters.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
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
	   void Write<T1, T2, T3, T4, T5, T6>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, Exception exception, in CallerInfo recordInfo  );


	
		/// <summary>
        /// Writes a log record with 7 parameters.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
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
	   void Write<T1, T2, T3, T4, T5, T6, T7>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, Exception exception, in CallerInfo recordInfo  );


	
		/// <summary>
        /// Writes a log record with 8 parameters.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
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
	   void Write<T1, T2, T3, T4, T5, T6, T7, T8>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, Exception exception, in CallerInfo recordInfo  );


	
		/// <summary>
        /// Writes a log record with 9 parameters.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
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
	   void Write<T1, T2, T3, T4, T5, T6, T7, T8, T9>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, Exception exception, in CallerInfo recordInfo  );


	
		/// <summary>
        /// Writes a log record with 10 parameters.
        /// </summary>
		/// <param name="context">The context for which the message has to be written.</param>
        /// <param name="level">The severity of the record (e.g. <see cref="FlashtraceLevel.Info"/> or <see cref="FlashtraceLevel.Warning"/>).</param>
        /// <param name="text">The text of the log record, including parameters (e.g. <c>Opening {Path} file {ShareMode} sharing mode</c>).</param>
        /// <param name="recordKind">Kind of record.</param>
        /// <param name="exception">The <see cref="Exception"/> associated with the record, or <c>null</c>.</param>
        /// <param name="recordInfo">Information about the caller source code.</param>
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
	   void Write<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>( ILoggingContext context, FlashtraceLevel level, LogRecordKind recordKind, string text, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, Exception exception, in CallerInfo recordInfo  );


		
	}
}