// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Spectre.Console.Cli;
using System;
using System.Threading.Tasks;

namespace Metalama.Backstage.Desktop.Windows.Commands;

public abstract class BaseAsyncCommand<T> : AsyncCommand<T>
    where T : BaseSettings
{
    public override async Task<int> ExecuteAsync( CommandContext context, T settings )
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
                logger.Error?.Log( e.ToString() );
                serviceProvider.GetBackstageService<IExceptionReporter>()?.ReportException( e );
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