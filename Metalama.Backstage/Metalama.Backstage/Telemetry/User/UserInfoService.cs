// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Telemetry.User;

[PublicAPI]
internal sealed class UserInfoService : IUserInfoService
{
    private readonly ILogger _logger;
    private readonly IExceptionReporter _exceptionReporter;
    private readonly UserInfoSource[] _orderedUserInfoSources;
    private readonly ConfigurationUserInfoSource _configurationUserInfoSource;

    public UserInfoService( IServiceProvider serviceProvider )
    {
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(UserInfoService) );
        this._exceptionReporter = serviceProvider.GetRequiredBackstageService<IExceptionReporter>();
        this._configurationUserInfoSource = new( serviceProvider );

        this._orderedUserInfoSources =
            new UserInfoSource[]
            {
                this._configurationUserInfoSource, new VisualStudioUserInfoSource(), new ActiveDirectoryUserInfoSource(), new WindowsProfileUserInfoSource()
            };
    }
    
    public bool TryGetUserInfo( [NotNullWhen( true )] out UserInfo? userInfo )
    {
        foreach ( var source in this._orderedUserInfoSources )
        {
            this._logger.Trace?.Log( $"Retrieving user information from {source.GetType().Name}." );
            
            try
            {
                if ( source.TryGetUserInfo( out userInfo ) )
                {
                    this._logger.Trace?.Log( $"User information retrieved from {source.GetType().Name}." );
                    
                    return true;
                }
            }
            catch ( Exception e )
            {
                try 
                {
                    this._exceptionReporter.ReportException( new InvalidOperationException( $"{source.GetType().Name} failed to retrieve user information.", e ) );
                }
                catch
                {
                    // We don't want to crash the application if we can't report the exception.
                }
            }
        }

        this._logger.Trace?.Log( "Failed to retrieve user information." );

        userInfo = null;
        
        return false;
    }

    public void SaveEmailAddress( string emailAddress ) => this._configurationUserInfoSource.SaveEmailAddress( emailAddress );
}