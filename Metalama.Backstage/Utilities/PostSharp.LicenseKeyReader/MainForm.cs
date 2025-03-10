// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses;

namespace PostSharp.LicenseKeyReader
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            this.InitializeComponent();
        }

        public void ShowError( string message )
        {
            MessageBox.Show( this, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error );
        }

        private void OnReadButtonClicked( object sender, EventArgs e )
        {
            this._propertyGrid.SelectedObject = null;

            if ( !LicenseKeyData.TryDeserialize( this._licenseKeyTextBox.Text, out var licenseKeyData, out var errorMessage ) )
            {
                this.ShowError( errorMessage );

                return;
            }

            this._propertyGrid.SelectedObject = licenseKeyData;

            if ( licenseKeyData.RequiresSignature()
                 && !licenseKeyData.VerifySignature( LicensingAuthority.GetProductionAuthority() ) )
            {
                this.ShowError( "Failed to verify the license key signature" );
            }
        }
    }
}