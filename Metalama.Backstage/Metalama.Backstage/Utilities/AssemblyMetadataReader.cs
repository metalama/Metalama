// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Metalama.Backstage.Utilities
{
    /// <summary>
    /// Reads the <see cref="AssemblyMetadataAttribute"/> defined by the build pipeline.
    /// These attributes are defined by Directory.Build.targets.
    /// </summary>
    [PublicAPI]
    public class AssemblyMetadataReader
    {
        private readonly Assembly _assembly;
        private readonly Dictionary<string, string?> _metadata = new( StringComparer.OrdinalIgnoreCase );
        
#pragma warning disable IDE0028
        private static readonly ConditionalWeakTable<Assembly, AssemblyMetadataReader> _instances = new();
#pragma warning restore IDE0028

        private bool _packageVersionRead;
        private string? _packageVersion;
        private bool _buildDateRead;
        private DateTime? _buildDate;

        public string? Company => this._assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;

        /// <summary>
        /// Gets the version of the package containing the current assembly.
        /// </summary>
        public string? PackageVersion
        {
            get
            {
                if ( !this._packageVersionRead )
                {
                    if ( this.TryGetValue( "PackageVersion", out var version ) )
                    {
                        this._packageVersion = version;
                    }

                    this._packageVersionRead = true;
                }

                return this._packageVersion;
            }
        }

        /// <summary>
        /// Gets the build date of the package containing the current assembly.
        /// </summary>
        public DateTime? BuildDate
        {
            get
            {
                if ( !this._buildDateRead )
                {
                    if ( this.TryGetValue( "PackageBuildDate", out var buildDateString ) )
                    {
                        this._buildDate = DateTime.Parse( buildDateString, CultureInfo.InvariantCulture );
                    }

                    this._buildDateRead = true;
                }

                return this._buildDate;
            }
        }

        private AssemblyMetadataReader( Assembly assembly )
        {
            this._assembly = assembly;

            foreach ( var attribute in assembly.GetCustomAttributes( typeof(AssemblyMetadataAttribute) ).Cast<AssemblyMetadataAttribute>() )
            {
                // In case of duplicates, we just ignore the first one. This happens with attributes describing package versions and when different assemblies are IL-merged.
                this._metadata[attribute.Key] = attribute.Value;
            }
        }

        /// <summary>
        /// Gets an <see cref="AssemblyMetadataReader"/> for a given <see cref="Assembly"/>.
        /// </summary>
        public static AssemblyMetadataReader GetInstance( Assembly assembly ) => _instances.GetValue( assembly, a => new AssemblyMetadataReader( a ) );

        public bool TryGetValue( string key, [MaybeNullWhen( false )] out string value ) => this._metadata.TryGetValue( key, out value ) && value != null;

        public string this[ string key ]
        {
            get
            {
                if ( !this.TryGetValue( key, out var value ) )
                {
                    throw new ArgumentOutOfRangeException( nameof(key), $"The assembly does not contain a metadata of key '{key}'." );
                }

                return value;
            }
        }

        /// <summary>
        /// Gets the package version with which the current assembly was built.
        /// </summary>
        public string GetPackageVersion( string packageName )
            => this.TryGetValue( "Package:" + packageName, out var version )
                ? version
                : throw new InvalidOperationException(
                    $"The AssemblyMetadataAttribute for package '{packageName}' is not defined in assembly '{this._assembly.GetName()}'." );

        /// <summary>
        /// Gets the unique BuildId for this assembly.
        /// </summary>
        public Guid ModuleId => this._assembly.ManifestModule.ModuleVersionId;

        /// <summary>
        /// Gets the major, minor, build, and revision numbers of the assembly.
        /// </summary>
        public Version AssemblyVersion
            => this._assembly.GetName().Version ?? throw new InvalidOperationException( $"Unknown version of assembly '{this._assembly.GetName()}'." );

        /// <summary>
        /// Gets the unique BuildId for the main assembly.
        /// </summary>
        public string BuildId
            => this.AssemblyVersion.ToString( 4 ) + "-" +
               string.Join( "", this.ModuleId.ToByteArray().Take( 4 ).Select( i => i.ToString( "x2", CultureInfo.InvariantCulture ) ) );
    }
}