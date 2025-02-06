// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// TODO: [Porting] Disabled: Logging in caching tests currently rely on TextLoggingBackend.

#if false
using Metalama.Patterns.Caching.TestHelpers;
using System;
using Xunit;

namespace Metalama.Patterns.Caching.Tests
{
    public class InitializationAndCleanup
    {
        [ModuleInitializer( 0 )]
        public static void AssemblyInitialize()
        {
            Trace.Listeners.Add( new TextWriterTraceListener( Console.Out ) );
        }
    }

    internal static class LoggingInitializer
    {
        [ModuleInitializer( 1 )]
        public static void Initialize()
        {
            Assert.False( LoggingServices.IsRuntimeLicenseExpired );
            LoggingServices.DefaultBackend = new ConsoleLoggingBackend();

            // By default, logging is disabled:
            Assert.False(
                LoggingServices.DefaultBackend.CurrentContextLocalConfiguration.Verbosity.GetSource( LoggingRoles.Caching, typeof(LoggingInitializer) )
                    .IsEnabled( FlashtraceLevel.Debug ) );

            // But we enable it:
            LoggingServices.DefaultBackend.DefaultContextLocalConfiguration.Verbosity.SetMinimalLevel( FlashtraceLevel.Debug, LoggingRoles.Caching );

            Assert.True(
                LoggingServices.DefaultBackend.CurrentContextLocalConfiguration.Verbosity.GetSource( LoggingRoles.Caching, typeof(LoggingInitializer) )
                    .IsEnabled( FlashtraceLevel.Debug ) );
        }
    }

    public class LoggingInCachingTest : IDisposable
    {
        [Fact]
        public void TestLoggingOutput()
        {
            var selfLoggingBackend = new SelfLoggingBackend();
            Flashtrace.
            LoggingServices.DefaultBackend = selfLoggingBackend;
            TestProfileConfigurationFactory.InitializeTestWithCachingBackend( "LoggingInCachingTest" );

            // By default, there is no logging.
            this.Return4();
            this.Return4();

            // But then we enable it:
            Assert.Equal( 0, selfLoggingBackend.RecordCount );
            LoggingServices.DefaultBackend.CurrentContextLocalConfiguration.Verbosity.SetMinimalLevel( FlashtraceLevel.Debug, LoggingRoles.Caching );
            this.Return4();
            this.Return4();
            Assert.NotEqual( 0, selfLoggingBackend.RecordCount );
        }

        public void Dispose()
        {
            TestProfileConfigurationFactory.DisposeTest();
            LoggingServices.DefaultBackend = null;
        }

        [Cache]
        private int Return4()
        {
            return 4;
        }

        public class SelfLoggingBackend : TextLoggingBackend
        {
            public int RecordCount { get; set; } = 0;

            protected override LoggingTypeSource CreateTypeSource( LoggingNamespaceSource parent, Type type )
            {
                return new SelfTypeSource( parent, type.FullName, type );
            }

            public override LogRecordBuilder CreateRecordBuilder()
            {
                return new SelfLogRecordBuilder( this );
            }

            protected override TextLoggingBackendOptions GetTextBackendOptions()
            {
                return new TextLoggingBackendOptions();
            }
        }

        public class SelfTypeSource : LoggingTypeSource
        {
#pragma warning disable 618
            public SelfTypeSource( LoggingNamespaceSource parent, string shortName, Type type ) : base( parent, type.FullName, type )
#pragma warning restore 618
            { }

            protected override bool IsBackendEnabled( FlashtraceLevel level )
            {
                return true;
            }
        }

        public class SelfLogRecordBuilder : TextLogRecordBuilder
        {
            private readonly SelfLoggingBackend backend;

            public SelfLogRecordBuilder( SelfLoggingBackend backend ) : base( backend )
            {
                this.backend = backend;
            }

            protected override void Write( UnsafeString message )
            {
                this.backend.RecordCount++;
            }
        }
    }
}
#endif