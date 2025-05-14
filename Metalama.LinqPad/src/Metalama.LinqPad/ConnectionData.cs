// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using LINQPad.Extensibility.DataContext;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Metalama.LinqPad
{
    /// <summary>
    /// Represents a connection, i.e. contains information about the loaded project or solution.
    /// </summary>
    internal sealed class ConnectionData : INotifyPropertyChanged
    {
        private string? _project;
        private string? _displayName;
        private bool _persist;
        private bool _reportWorkspaceErrors = true;

        public ConnectionData( IConnectionInfo connectionInfo )
        {
            if ( connectionInfo.DriverData != null )
            {
                this.Project = connectionInfo.DriverData.Element( "Project" )?.Value;

                this.ReportWorkspaceErrors = connectionInfo.DriverData.Element( "ReportWorkspaceErrors" )?.Value.ToLowerInvariant() == "true";
            }

            this.DisplayName = connectionInfo.DisplayName;
            this.Persist = connectionInfo.Persist;
        }

        public ConnectionData() { }

        public string? Project
        {
            get => this._project;
            set
            {
                this._project = value;
                this.OnPropertyChanged();
            }
        }

        public string DisplayName
        {
            get => string.IsNullOrWhiteSpace( this._displayName ) ? Path.GetFileName( this.Project )! : this._displayName;

            set
            {
                this._displayName = value;
                this.OnPropertyChanged();
            }
        }

        public bool Persist
        {
            get => this._persist;
            set
            {
                this._persist = value;
                this.OnPropertyChanged();
            }
        }

        public bool ReportWorkspaceErrors
        {
            get => this._reportWorkspaceErrors;

            set
            {
                this._reportWorkspaceErrors = value;
                this.OnPropertyChanged();
            }
        }

        public void Save( IConnectionInfo connectionInfo )
        {
            connectionInfo.DisplayName = this.DisplayName;

            connectionInfo.DriverData = new XElement(
                "MetalamaConnection",
                new XElement( "Project", this.Project ),
                new XElement( "ReportWorkspaceErrors", this.ReportWorkspaceErrors ) );

            connectionInfo.Persist = this.Persist;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged( [CallerMemberName] string? propertyName = null )
        {
            this.PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
        }
    }
}