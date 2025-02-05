// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Spectre.Console.Cli;
using System;

namespace Metalama.Backstage.Desktop.Windows.Commands;

public abstract class BaseCommand<T> : Command<T>
    where T : BaseSettings
{
    public override int Execute( CommandContext context, T settings )
    {
        var serviceProvider = App.GetBackstageServices( settings );
        var loggerFactory = serviceProvider.GetLoggerFactory();
        var logger = loggerFactory.GetLogger( this.GetType().Name );
        logger.Trace?.Log( $"Executing command {this.GetType().Name}" );

        try
        {
            var result = this.Execute( new ExtendedCommandContext( context, serviceProvider, logger ), settings );
            
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

    protected abstract int Execute( ExtendedCommandContext context, T settings );
}