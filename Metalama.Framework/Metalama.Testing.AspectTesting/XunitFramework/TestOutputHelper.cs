// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Globalization;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    internal sealed class TestOutputHelper : ITestOutputHelper
    {
        private readonly IMessageSink _messageSink;
        private readonly ITest _test;
        private readonly StringBuilder _stringBuilder = new();

        public TestOutputHelper( IMessageSink messageSink, ITest test )
        {
            this._messageSink = messageSink;
            this._test = test;
        }

        public void WriteLine( string message )
        {
            var line = message + Environment.NewLine;
            this._messageSink.OnMessage( new TestOutput( this._test, line ) );
            this._stringBuilder.Append( line );
        }

        // ReSharper disable once RedundantStringFormatCall
        public void WriteLine( string format, params object[] args ) => this.WriteLine( string.Format( CultureInfo.InvariantCulture, format, args ) );

        public override string ToString() => this._stringBuilder.ToString();
    }
}