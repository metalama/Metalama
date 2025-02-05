// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Compiler.Services;
using System;

namespace Metalama.Framework.Engine.Services;

internal sealed class ExceptionReporterAdapter : IExceptionReporter
{
    private readonly Backstage.Telemetry.IExceptionReporter? _backstageReporter;

    public ExceptionReporterAdapter( Backstage.Telemetry.IExceptionReporter? backstageReporter )
    {
        this._backstageReporter = backstageReporter;
    }

    public void ReportException( Exception reportedException ) => this._backstageReporter?.ReportException( reportedException );
}