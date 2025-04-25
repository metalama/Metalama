// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Metalama.Backstage.Licensing.Licenses;

namespace PostSharp.LicenseKeyGenerator
{
    internal sealed partial class MainForm : Form
    {
        private readonly LicensingAuthority _authority;

        public MainForm()
        {
            this.InitializeComponent();

            this._propertyGrid.SelectedObject = new LicenseKeyDataBuilder()
            {
                Generation = LicenseGeneration.Current
            };

            // We load the private key on startup to avoid KeyVault exceptions after filling all the data.
            const string keyVaultUri = "https://postsharpbusinesssystkv.vault.azure.net/";
            var client = new SecretClient( new Uri( keyVaultUri ), new DefaultAzureCredential() );
            var privateKey0 = client.GetSecret( "Licensing-PrivateKey0" ).Value.Value;
            this._authority = new LicensingAuthority( (0, privateKey0) );
        }

        private void OnSerializedButtonClicked( object sender, EventArgs e )
        {
            var licenseKeyBuilder = (LicenseKeyDataBuilder) this._propertyGrid.SelectedObject;
            var licenseKey = licenseKeyBuilder.SignAndSerialize( this._authority );

            if ( !LicenseKeyData.TryDeserialize( licenseKey, out var deserializedLicenseKeyData, out var errorMessage ) )
            {
                throw new InvalidOperationException( errorMessage );
            }

            if ( !deserializedLicenseKeyData.VerifySignature( this._authority ) )
            {
                throw new InvalidOperationException( "Failed to verify license signature." );
            }

            this._propertyGrid.SelectedObject = licenseKeyBuilder;
        }
    }
}