// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Framework.DesignTime.Pipeline;
using Metalama.Framework.DesignTime.Utilities;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.CompileTime.Manifest;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;

namespace Metalama.Framework.DesignTime.Diagnostics
{
    /// <summary>
    /// Allows to register user diagnostics and suppressions for storage in the user profile, and read this file.
    /// </summary>
    internal sealed class UserDiagnosticRegistrationService : IUserDiagnosticRegistrationService
    {
        // Multiple instances are needed for testing.
        private readonly UserDiagnosticsConfiguration _registrationFile;
        private readonly IConfigurationManager _configurationManager;
        private readonly DesignTimeExceptionHandler _exceptionHandler;

        public UserDiagnosticRegistrationService( GlobalServiceProvider serviceProvider )
        {
            this._exceptionHandler = serviceProvider.GetRequiredService<DesignTimeExceptionHandler>();
            this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
            this._registrationFile = this._configurationManager.Get<UserDiagnosticsConfiguration>();
        }

        public bool ShouldWrapUnsupportedDiagnostics => true;

        [Memo]
        public DesignTimeDiagnosticDefinitions DiagnosticDefinitions
            => new(
                this._registrationFile.Diagnostics.SelectAsImmutableArray( d => d.Value.DiagnosticDescriptor() ),
                this._registrationFile.Suppressions.SelectAsImmutableArray( SuppressionFactories.CreateDescriptor ) );

        /// <summary>
        /// Inspects a <see cref="DesignTimePipelineExecutionResult"/> and compares the reported or suppressed diagnostics to the list of supported diagnostics
        /// and suppressions from the user profile. If some items are not supported in the user profile, add them to the user profile. 
        /// </summary>
        public void RegisterDescriptors( DiagnosticManifest diagnosticManifest )
        {
            try
            {
                this._configurationManager.UpdateIf<UserDiagnosticsConfiguration>(
                    f =>
                    {
                        var missing = GetMissingDiagnostics( f, diagnosticManifest );

                        return missing.Diagnostics.Count > 0 || missing.Suppressions.Count > 0;
                    },
                    f =>
                    {
                        var missing = GetMissingDiagnostics( f, diagnosticManifest );

                        return f with
                        {
                            Diagnostics = f.Diagnostics.AddRange(
                                missing.Diagnostics.SelectAsReadOnlyCollection(
                                    d => new KeyValuePair<string, UserDiagnosticRegistration>( d.Id, new UserDiagnosticRegistration( d ) ) ) ),
                            Suppressions = f.Suppressions.AddRange( missing.Suppressions )
                        };
                    } );
            }
            catch ( Exception e )
            {
                // We swallow exceptions because we don't want to fail the pipeline in case of error here.
                this._configurationManager.Logger.Error?.Log( $"Cannot register user diagnostics and registrations: {e.Message}." );
                this._exceptionHandler.ReportException( e );
            }
        }

        private static (List<string> Suppressions, List<IDiagnosticDefinition> Diagnostics) GetMissingDiagnostics(
            UserDiagnosticsConfiguration file,
            DiagnosticManifest diagnosticManifest )
        {
            List<string> missingSuppressions = new();
            List<IDiagnosticDefinition> missingDiagnostics = new();

            foreach ( var suppression in diagnosticManifest.SuppressionDefinitions.Select( s => s.SuppressedDiagnosticId )
                         .Distinct() )
            {
                if ( !file.Suppressions.Contains( suppression ) )
                {
                    missingSuppressions.Add( suppression );
                }
            }

            foreach ( var diagnostic in diagnosticManifest.DiagnosticDefinitions )
            {
                if ( !DesignTimeDiagnosticDefinitions.StandardDiagnosticDescriptors.ContainsKey( diagnostic.Id )
                     && !file.Diagnostics.ContainsKey( diagnostic.Id ) )
                {
                    // Duplicates here would cause an exception in RegisterDescriptors.
                    // They can happen if the diagnostic is defined as a property, since it is reported from its backing field as well.
                    // Also, the case where the same diagnostic ID is defined in multiple places shouldn't throw either.
                    if ( missingDiagnostics.All( d => d.Id != diagnostic.Id ) )
                    {
                        missingDiagnostics.Add( diagnostic );
                    }
                }
            }

            return (missingSuppressions, missingDiagnostics);
        }
    }
}