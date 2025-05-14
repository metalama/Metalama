// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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

            this.InitializeComponent();

            // DataContext must be set after InitializeComponent is called
            // because we need to override design-time data.
            this.DataContext = this._connectionData;
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