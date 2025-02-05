// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Metalama.Backstage.Licensing.Licenses;

namespace PostSharp.LicenseKeyGenerator
{
    public partial class MainForm : Form
    {
        private readonly string _privateKey;

        public MainForm()
        {
            this.InitializeComponent();
            this._propertyGrid.SelectedObject = new LicenseKeyDataBuilder();

            // We load the private key on startup to avoid KeyVault exceptions after filling all the data.
            var kvUri = "https://postsharpbusinesssystkv.vault.azure.net/";
            var client = new SecretClient( new Uri( kvUri ), new DefaultAzureCredential() );
            this._privateKey = client.GetSecret( "Licensing-PrivateKey0" ).Value.Value;
        }

        private void OnSerializedButtonClicked( object sender, EventArgs e )
        {
            var licenseKeyData = (LicenseKeyDataBuilder) this._propertyGrid.SelectedObject;
            var licenseKey = licenseKeyData.SignAndSerialize( 0, this._privateKey );

            if ( !LicenseKeyData.TryDeserialize( licenseKey, out var deserializedLicenseKeyData, out var errorMessage ) )
            {
                throw new InvalidOperationException( errorMessage );
            }

            if ( !deserializedLicenseKeyData.HasValidSignature )
            {
                throw new InvalidOperationException( "Failed to verify license signature." );
            }

            this._propertyGrid.SelectedObject = licenseKeyData;
        }
    }
}