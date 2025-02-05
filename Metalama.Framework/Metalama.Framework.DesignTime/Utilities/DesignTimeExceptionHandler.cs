// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Diagnostics;
using Metalama.Framework.Services;

namespace Metalama.Framework.DesignTime.Utilities
{
    public sealed class DesignTimeExceptionHandler : IGlobalService
    {
        private readonly IExceptionReporter? _exceptionReporter;

        public DesignTimeExceptionHandler( ServiceProvider<IGlobalService> serviceProvider )
        {
            this._exceptionReporter = serviceProvider.GetBackstageService<IExceptionReporter>();
        }

        // It is critical that OperationCanceledException is NOT handled, i.e. this exception should flow to the caller, otherwise VS will be satisfied
        // with the incomplete results it received, and cache them. 
        internal static bool MustHandle( Exception e )
            => e switch
            {
                OperationCanceledException => false,
                AggregateException { InnerException: not null } => MustHandle( e.InnerException ),
                _ => true
            };

        internal void ReportException( Exception e, ILogger? logger = null )
        {
            logger ??= Logger.DesignTime;

            if ( MustHandle( e ) )
            {
                logger.Error?.Log( e.ToString() );

                // TODO: Is this guaranteed not to be called before the BackstageServiceFactory is initialized?
                var exceptionReporter = this._exceptionReporter ?? BackstageServiceFactory.ServiceProvider.GetBackstageService<IExceptionReporter>();

                exceptionReporter?.ReportException( e );
            }
            else
            {
                logger.Warning?.Log( $"Got an acceptable exception {e.GetType().Name}." );
            }
        }
    }
}