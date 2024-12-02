// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Telemetry;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Engine.Services;
using Metalama.Testing.UnitTesting;
using StreamJsonRpc;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests;

public abstract class FrameworkBaseTestClass : UnitTestClass, IDisposable
{
    private readonly TestExceptionReporter _exceptionReporter;

    protected FrameworkBaseTestClass( ITestOutputHelper? logger = null ) : base( logger )
    {
        this._exceptionReporter = new TestExceptionReporter();
    }

    public void Dispose()
    {
        // We generally don't want to see any exceptions reported during the test.
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        Assert.DoesNotContain( this._exceptionReporter.ReportedExceptions, e => e is not ConnectionLostException );
    }

    protected override void ConfigureServices( IAdditionalServiceCollection services )
    {
        base.ConfigureServices( services );
        ((AdditionalServiceCollection) services).BackstageServices.Add( this._exceptionReporter );
        services.AddGlobalService( provider => new DesignTimeExceptionHandler( provider ) );
    }

    private sealed class TestExceptionReporter : IExceptionReporter
    {
        private readonly ConcurrentBag<Exception> _reportedExceptions = new ConcurrentBag<Exception>();

        public IReadOnlyCollection<Exception> ReportedExceptions => this._reportedExceptions;

        public void ReportException( Exception reportedException, ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception, string? localReportPath = null, IExceptionAdapter? exceptionAdapter = null )
        {
            this._reportedExceptions.Add( reportedException );
        }
    }
}
