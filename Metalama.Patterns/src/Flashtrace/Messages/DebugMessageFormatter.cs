// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Records;
using System.Text;

namespace Flashtrace.Messages;

internal static class DebugMessageFormatter
{
    public static string Format<T>( T message )
        where T : IMessage
    {
        using var writer = new Writer();
        message.Write( writer, LogRecordItem.Message );

        return writer.ToString();
    }

    private sealed class Writer : ILogRecordBuilder
    {
        private readonly StringBuilder _stringBuilder = new();

        public void BeginWriteItem( LogRecordItem item, in LogRecordTextOptions options ) { }

        public void Complete() { }

        public void Dispose() { }

        public void EndWriteItem( LogRecordItem item ) { }

        public void SetException( Exception exception )
        {
            this._stringBuilder.Append( ", " + exception.GetType().Name + " : " + exception.Message );
        }

        public void SetExecutionTime( double executionTime, bool isOvertime ) { }

        public void WriteParameter<T>( int index, in ReadOnlySpan<char> parameterName, T? value, in LogParameterOptions options )
        {
            this._stringBuilder.Append( value?.ToString() );
        }

        public void WriteString( in ReadOnlySpan<char> str )
        {
            this._stringBuilder.Append( str.ToString() );
        }

        public override string ToString() => this._stringBuilder.ToString();
    }
}