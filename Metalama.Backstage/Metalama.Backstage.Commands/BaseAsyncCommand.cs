// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Spectre.Console.Cli;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Metalama.Backstage.Commands;

[PublicAPI]
[UsedImplicitly( ImplicitUseTargetFlags.WithInheritors )]
public abstract class BaseAsyncCommand<T> : AsyncCommand<T>
    where T : BaseCommandSettings
{
#pragma warning disable CS8765
    public override async Task<int> ExecuteAsync( CommandContext context, T settings )
#pragma warning restore CS8765
    {
        if ( settings.Debug )
        {
            Debugger.Launch();
        }

        var extendedContext = new ExtendedCommandContext( context, settings, this.AddBackstageOptions );
        var logger = extendedContext.ServiceProvider.GetLoggerFactory().GetLogger( this.GetType().Name );
        logger.Trace?.Log( $"Executing command {this.GetType().Name}" );

        try
        {
            // We don't report usage of commands.

            await this.ExecuteAsync( extendedContext, settings );
            logger.Trace?.Log( $"The command succeeded." );

            return 0;
        }
        catch ( CommandException e )
        {
            logger.Error?.Log( e.Message );
            
            return e.ReturnCode;
        }
        catch ( Exception e )
        {
            try
            {
                logger.Error?.Log( e.ToString() );
                extendedContext.ServiceProvider.GetBackstageService<IExceptionReporter>()?.ReportException( e );
            }
            catch ( Exception reporterException )
            {
                throw new AggregateException( e, reporterException );
            }

            throw;
        }
    }

    protected abstract Task ExecuteAsync( ExtendedCommandContext context, T settings );

    protected virtual BackstageInitializationOptions AddBackstageOptions( BackstageInitializationOptions options ) => options;
}