// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using LINQPad.Extensibility.DataContext;
using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace Metalama.LinqPad
{
    /// <summary>
    /// Interaction logic for ConnectionDialog.xaml.
    /// </summary>
    public sealed partial class ConnectionDialog
    {
        private readonly IConnectionInfo _connectionInfo;
        private readonly ConnectionData _connectionData;

        public ConnectionDialog( IConnectionInfo cxInfo )
        {
            this._connectionInfo = cxInfo;
            this._connectionData = new ConnectionData( cxInfo );
            this.DataContext = this._connectionData;
            this.InitializeComponent();
        }

        private void OnOkButtonClick( object sender, RoutedEventArgs e )
        {
            this._connectionData.Save( this._connectionInfo );
            this.DialogResult = true;
        }

        private void BrowseProject( object sender, RoutedEventArgs e )
        {
            var dialog = new OpenFileDialog()
            {
                Title = "Choose project or solution",
                DefaultExt = ".csproj",
                CheckFileExists = true,
                Filter = "Project files (*.csproj, *.sln)|*.csproj;*.sln"
            };

            if ( dialog.ShowDialog() == true )
            {
                var fileName = dialog.FileName.Trim( '\"' );
                this._connectionInfo.DisplayName = Path.GetFileName( fileName );
                this._connectionData.Project = fileName;
            }
        }
    }
}