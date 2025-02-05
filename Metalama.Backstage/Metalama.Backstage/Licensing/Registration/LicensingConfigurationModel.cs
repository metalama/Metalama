// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Licensing.Licenses;
using System;

namespace Metalama.Backstage.Licensing.Registration
{
    /// <summary>
    /// Manages license file for license registration purposes.
    /// </summary>
    [PublicAPI]
    internal class LicensingConfigurationModel
    {
        private readonly IServiceProvider _services;
        private readonly IConfigurationManager _configurationManager;
        private readonly IDateTimeProvider _dateTimeProvider;

        private LicensingConfiguration _configuration = null!;

        private LicensingConfigurationModel( IServiceProvider services )
        {
            this._services = services;
            this._dateTimeProvider = services.GetRequiredBackstageService<IDateTimeProvider>();
            this._configurationManager = services.GetRequiredBackstageService<IConfigurationManager>();
            this._configurationManager.ConfigurationFileChanged += this.OnConfigurationFileChanged;

            this.ReadConfiguration();
        }

        private void ReadConfiguration()
        {
            var configurationManager = this._services.GetRequiredBackstageService<IConfigurationManager>();
            this._configuration = configurationManager.Get<LicensingConfiguration>();
            this.LicenseString = this._configuration.License;

            if ( string.IsNullOrWhiteSpace( this.LicenseString ) )
            {
                this.LicenseProperties = null;
                this.LicenseString = null;
            }
            else
            {
                var licenseFactory = new LicenseFactory( this._services );

                if ( licenseFactory.TryCreate( this.LicenseString, out var license, out _ ) )
                {
                    _ = license.TryGetProperties( out var licenseRegistrationData, out _ );
                    this.LicenseProperties = licenseRegistrationData;
                }
                else
                {
                    this.LicenseProperties = null;
                }
            }
        }

        private void OnConfigurationFileChanged( ConfigurationFile file )
        {
            if ( file is LicensingConfiguration )
            {
                this.ReadConfiguration();
                this.Changed?.Invoke();
            }
        }

        public event Action? Changed;

        /// <summary>
        /// Gets the license contained in the storage or to be stored to the storage.
        /// </summary>
        public string? LicenseString { get; private set; }

        /// <summary>
        /// Gets the registration data of the license contained in the storage or to be stored to the storage.
        /// </summary>
        public LicenseProperties? LicenseProperties { get; private set; }

        public DateTime? LastEvaluationStartDate
        {
            get => this._configuration.LastEvaluationStartDate;
            set => this._configurationManager.Update<LicensingConfiguration>( c => c with { LastEvaluationStartDate = value } );
        }

        public bool IsEvaluationActive
            => this.LicenseProperties is { LicenseType: LicenseType.Evaluation } &&
               this.LicenseProperties?.ValidFrom == this._dateTimeProvider.UtcNow.Date;

        public bool CanStartEvaluation => this.NextEvaluationStartDate <= this._dateTimeProvider.UtcNow;

        public DateTime NextEvaluationStartDate
        {
            get
            {
                if ( this.LastEvaluationStartDate != null )
                {
                    return
                        this.LastEvaluationStartDate.Value + LicensingConstants.NoEvaluationPeriod + LicensingConstants.EvaluationPeriod;
                }
                else
                {
                    return this._dateTimeProvider.UtcNow;
                }
            }
        }

        /// <summary>
        /// Opens a license file or creates empty storage if the license file doesn't exist.
        /// </summary>
        /// <param name="services">Services.</param>
        public static LicensingConfigurationModel Create( IServiceProvider services )
        {
            return new LicensingConfigurationModel( services );
        }

        /// <summary>
        /// Stores a license.
        /// </summary>
        /// <param name="licenseString">String representing the license to be stored in the license file.</param>
        /// <param name="data">Data represented by the <paramref name="licenseString"/>.</param>
        public void SetLicense( string licenseString, LicenseProperties data )
        {
            this.LicenseString = licenseString;
            this.LicenseProperties = data;
            this.Save();
        }

        /// <summary>
        /// Removes the stored license.
        /// </summary>
        /// <returns>
        /// <c>true</c> if a license has been removed, <c>false</c> if there was no license to be removed.
        /// </returns>
        public bool RemoveLicense()
        {
            if ( this.LicenseString != null )
            {
                this.LicenseString = null;
                this.LicenseProperties = null;
                this.Save();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Writes the licenses to a file.
        /// </summary>
        /// <remarks>
        /// Overwrites existing file.
        /// </remarks>
        private void Save()
        {
            // The ConfigurationManager may trigger the OnConfigurationFileChanged event when saving.
            var licenseString = this.LicenseString;

            this._configurationManager
                .Update<LicensingConfiguration>( c => c with { License = licenseString } );
        }
    }
}