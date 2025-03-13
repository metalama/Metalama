// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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