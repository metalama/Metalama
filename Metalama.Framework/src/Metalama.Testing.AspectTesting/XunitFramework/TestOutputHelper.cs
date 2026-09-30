// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;
using System.Text;
using Xunit;
using Xunit.Sdk;
using Xunit.v3;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    /// <summary>
    /// Sends the output of a test to the message sink of xunit as <see cref="TestOutput"/> messages, and accumulates it
    /// so that it can be attached to the result of the test.
    /// </summary>
    internal sealed class TestOutputHelper : ITestOutputHelper
    {
        /// <summary>
        /// The sink that receives the output messages.
        /// </summary>
        private readonly IMessageSink _messageSink;
        /// <summary>
        /// The test that writes the output.
        /// </summary>
        private readonly Test _test;
        /// <summary>
        /// The output written so far.
        /// </summary>
        private readonly StringBuilder _stringBuilder = new();
        /// <summary>
        /// The lock that serializes the writes.
        /// </summary>
        private readonly object _sync = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TestOutputHelper"/> class.
        /// </summary>
        public TestOutputHelper( IMessageSink messageSink, Test test )
        {
            this._messageSink = messageSink;
            this._test = test;
        }

        /// <inheritdoc />
        public string Output
        {
            get
            {
                lock ( this._sync )
                {
                    return this._stringBuilder.ToString();
                }
            }
        }

        /// <inheritdoc />
        public void Write( string message )
        {
            lock ( this._sync )
            {
                this._stringBuilder.Append( message );
            }

            this._messageSink.OnMessage( TestMessages.Output( this._test, message ) );
        }

        // ReSharper disable once RedundantStringFormatCall
        /// <inheritdoc />
        public void Write( string format, params object[] args ) => this.Write( string.Format( CultureInfo.InvariantCulture, format, args ) );

        /// <inheritdoc />
        public void WriteLine( string message ) => this.Write( message + Environment.NewLine );

        // ReSharper disable once RedundantStringFormatCall
        /// <inheritdoc />
        public void WriteLine( string format, params object[] args ) => this.WriteLine( string.Format( CultureInfo.InvariantCulture, format, args ) );

        /// <inheritdoc />
        public override string ToString() => this.Output;
    }
}
