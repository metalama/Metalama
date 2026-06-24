// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Spectre.Console.Cli;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Backstage.Desktop.Windows.Commands;

[PublicAPI]
public abstract class BaseAsyncCommand<T> : AsyncCommand<T>
    where T : BaseSettings
{
    protected override async Task<int> ExecuteAsync( CommandContext context, T settings, CancellationToken cancellationToken )
    {
        var serviceProvider = App.GetBackstageServices( settings );
        var loggerFactory = serviceProvider.GetLoggerFactory();
        var logger = loggerFactory.GetLogger( this.GetType().Name );
        logger.Trace?.Log( $"Executing command {this.GetType().Name}" );

        try
        {
            var result = await this.ExecuteAsync( new ExtendedCommandContext( context, serviceProvider, logger ), settings );

            return result;
        }
        catch ( Exception e )
        {
            try
            {
                var classifiedException = ExceptionClassifier.Classify( e );
                logger.LogException( classifiedException );

                // A CLI crash is telemetry about the tooling itself: report through the tooling policy. See #1701.
                serviceProvider.ReportToolingException( classifiedException );
            }
            catch ( Exception reporterException )
            {
                throw new AggregateException( e, reporterException );
            }

            throw;
        }
    }

    protected abstract Task<int> ExecuteAsync( ExtendedCommandContext context, T settings );
}