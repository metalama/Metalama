// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Loggers;
using Flashtrace.Messages;
using System.Diagnostics;
using System.Globalization;
using Xunit;

namespace Flashtrace.UnitTests;

public sealed class TestTraceSourceLogger : IClassFixture<TestTraceSourceLogger.TraceSourceFixture>
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class TraceSourceFixture : IDisposable
    {
        private readonly TraceSource _traceSource = TraceSourceFlashtraceLogger.GetTraceSource( FlashtraceRole.Default );

        internal MyListener Listener { get; } = new();

        internal FlashtraceSource Source { get; } = new TraceSourceLoggerFactory().GetFlashtraceSource( "test" );

        public TraceSourceFixture()
        {
            this._traceSource.Listeners.Add( this.Listener );
            this._traceSource.Switch.Level = SourceLevels.All;
        }

        public void Dispose()
        {
            this.Listener.Dispose();
        }
    }

    private readonly TraceSourceFixture _fixture;

    public TestTraceSourceLogger( TraceSourceFixture fixture )
    {
        this._fixture = fixture;
        this._fixture.Listener.Messages.Clear();
    }

    [Fact]
    public void Test_TextLogger_Write()
    {
        this._fixture.Source.WithLevel( FlashtraceLevel.Error ).Write( FormattedMessageBuilder.Formatted( "Error" ) );
        this._fixture.Source.WithLevel( FlashtraceLevel.Warning ).Write( FormattedMessageBuilder.Formatted( "Warning" ) );
        this._fixture.Source.WithLevel( FlashtraceLevel.Critical ).Write( FormattedMessageBuilder.Formatted( "Critical" ) );
        this._fixture.Source.WithLevel( FlashtraceLevel.Debug ).Write( FormattedMessageBuilder.Formatted( "Debug" ) );
        this._fixture.Source.WithLevel( FlashtraceLevel.Trace ).Write( FormattedMessageBuilder.Formatted( "Trace" ) );
        Assert.Equal( 5, this._fixture.Listener.Messages.Count );
        Assert.Equal( "Error", this._fixture.Listener.Messages[0] );
    }

    [Fact]
    public void Test_TextLogger_ActivitySuccess()
    {
        using ( var activity = this._fixture.Source.Default.OpenActivity( FormattedMessageBuilder.Formatted( "Activity" ) ) )
        {
            activity.SetSuccess();
        }

        Assert.Equal( 2, this._fixture.Listener.Messages.Count );
        Assert.Equal( "Activity: Starting", this._fixture.Listener.Messages[0] );
        Assert.Equal( "Activity: Succeeded", this._fixture.Listener.Messages[1] );
    }

    [Fact]
    public void Test_TextLogger_ActivityFailure()
    {
        using ( var activity = this._fixture.Source.Default.OpenActivity( FormattedMessageBuilder.Formatted( "Activity" ) ) )
        {
            activity.SetOutcome( FlashtraceLevel.Error, FormattedMessageBuilder.Formatted( "Oops: {Hello}", "Hello" ) );
        }

        Assert.Equal( 2, this._fixture.Listener.Messages.Count );
        Assert.Equal( "Activity: Starting", this._fixture.Listener.Messages[0] );
        Assert.Equal( "Activity: Returning, Oops: Hello", this._fixture.Listener.Messages[1] );
    }

    [Fact]
    public void Test_SemanticLogger_Write()
    {
        this._fixture.Source.WithLevel( FlashtraceLevel.Error ).Write( SemanticMessageBuilder.Semantic( "Error", ("a", "b"), ("b", "c") ) );
        this._fixture.Source.WithLevel( FlashtraceLevel.Warning ).Write( SemanticMessageBuilder.Semantic( "Warning", ("a", "b") ) );
        this._fixture.Source.WithLevel( FlashtraceLevel.Critical ).Write( SemanticMessageBuilder.Semantic( "Critical", ("a", "b") ) );
        this._fixture.Source.WithLevel( FlashtraceLevel.Debug ).Write( SemanticMessageBuilder.Semantic( "Debug", ("a", "b") ) );

        // NB: Had to add the Semantic( string name, in (string Name, object Value) parameter1 ) overload to get this to
        //     resolve correctly, otherwise the compiler was trying (incorrectly) to use the
        //     Semantic(string messageName, (string Name, object Value)[] parameters) overload (note there's no 'params')
        //     instead of the expected overload Semantic<T>( string name, in (string Name, T Value) parameter1 ) where T
        //     is object.
        this._fixture.Source.WithLevel( FlashtraceLevel.Trace ).Write( SemanticMessageBuilder.Semantic( "Trace", ("a", null) ) );
        Assert.Equal( 5, this._fixture.Listener.Messages.Count );
        Assert.Equal( "Error, a = b, b = c", this._fixture.Listener.Messages[0] );
    }

    [Fact]
    public void Test_SemanticLogger_ActivitySuccess()
    {
        using ( var activity = this._fixture.Source.Default.OpenActivity( SemanticMessageBuilder.Semantic( "Activity" ) ) )
        {
            activity.SetSuccess();
        }

        Assert.Equal( 2, this._fixture.Listener.Messages.Count );
        Assert.Equal( "Activity: Starting", this._fixture.Listener.Messages[0] );
        Assert.Equal( "Activity: Succeeded", this._fixture.Listener.Messages[1] );
    }

    [Fact]
    public void Test_SemanticLogger_ActivityFailure()
    {
        using ( var activity = this._fixture.Source.Default.OpenActivity( SemanticMessageBuilder.Semantic( "Activity", ("a", "b") ) ) )
        {
            activity.SetOutcome( FlashtraceLevel.Error, SemanticMessageBuilder.Semantic( "Failed", ("c", "d") ) );
        }

        Assert.Equal( 2, this._fixture.Listener.Messages.Count );
        Assert.Equal( "Activity, a = b: Starting", this._fixture.Listener.Messages[0] );
        Assert.Equal( "Activity, a = b: Failed, c = d", this._fixture.Listener.Messages[1] );
    }

    // ReSharper disable RedundantOverriddenMember

    internal sealed class MyListener : TraceListener
    {
        private string _buffer = "";

        public List<string?> Messages { get; } = new();

        public override void Write( string? message )
        {
            this._buffer += message;
        }

        public override void WriteLine( string? message )
        {
            this.Messages.Add( this._buffer + message );
            this._buffer = "";
        }

        public override void TraceEvent( TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message )
        {
            this.Messages.Add( message );
        }

        public override void TraceEvent( TraceEventCache? eventCache, string source, TraceEventType eventType, int id )
        {
#if NETFRAMEWORK
            base.TraceEvent( eventCache!, source, eventType, id );
#else
            base.TraceEvent( eventCache, source, eventType, id );
#endif
        }

        public override void TraceEvent( TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args )
        {
            this.Messages.Add( string.Format( CultureInfo.InvariantCulture, format!, args! ) );
        }
    }

    // ReSharper restore RedundantOverriddenMember
}