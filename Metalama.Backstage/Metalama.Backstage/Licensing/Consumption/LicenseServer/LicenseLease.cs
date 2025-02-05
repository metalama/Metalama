// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Infrastructure;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml;

namespace Metalama.Backstage.Licensing.Consumption.LicenseServer
{
    // TODO (this is just a copy from PostSharp.)

    /// <summary>
    /// Represents a lease of license by a specific user.
    /// </summary>
    [Serializable]
    public sealed class LicenseLease
    {
        /// <summary>
        /// Deserializes a string into a new <see cref="LicenseLease"/>.
        /// </summary>
        /// <param name="serializedLicenseLease">A serialized <see cref="LicenseLease"/>, produced by <see cref="Serialize"/>.</param>
        /// <returns>The <see cref="LicenseLease"/> built from <paramref name="serializedLicenseLease"/>, or <c>null</c> 
        /// if the string could not be deserialized.</returns>
        public static bool TryDeserialize(
            string serializedLicenseLease,
            IDateTimeProvider dateTimeProvider,
            [MaybeNullWhen( false )] out LicenseLease lease )
        {
            try
            {
                lease = new LicenseLease();
                var parts = serializedLicenseLease.Split( ';' );
                DateTime? startTime = null, endTime = null, renewTime = null;

                foreach ( var part in parts )
                {
#pragma warning disable CA1307
                    var pos = part.IndexOf( ':' );
#pragma warning restore CA1307

                    if ( pos < 0 || pos == part.Length - 1 )
                    {
                        continue;
                    }

                    var key = part.Substring( 0, pos ).Trim();
                    var value = part.Substring( pos + 1, part.Length - pos - 1 ).Trim();

                    // ReSharper disable StringLiteralTypo

                    switch ( key.ToLowerInvariant() )
                    {
                        case "license":
                            lease.LicenseString = value;

                            break;

                        case "starttime":
                            startTime = XmlConvert.ToDateTime( value, XmlDateTimeSerializationMode.Utc ).ToLocalTime();

                            break;

                        case "endtime":
                            endTime = XmlConvert.ToDateTime( value, XmlDateTimeSerializationMode.Utc ).ToLocalTime();

                            break;

                        case "renewtime":
                            renewTime = XmlConvert.ToDateTime( value, XmlDateTimeSerializationMode.Utc ).ToLocalTime();

                            break;
                    }

                    // ReSharper restore StringLiteralTypo
                }

                if ( lease.LicenseString == null )
                {
                    lease = null;

                    return false;
                }

                if ( !startTime.HasValue )
                {
                    startTime = dateTimeProvider.UtcNow;
                }

                if ( !endTime.HasValue )
                {
                    endTime = startTime.Value.AddDays( 1 );
                }

                if ( !renewTime.HasValue )
                {
                    renewTime = endTime;
                }

                lease.StartTime = startTime.Value;
                lease.EndTime = endTime.Value;
                lease.RenewTime = renewTime.Value;

                return true;
            }
            catch ( FormatException )
            {
                lease = null;

                return false;
            }
        }

        private LicenseLease() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LicenseLease"/> class.
        /// </summary>
        /// <param name="licenseString">String serialization of the license (license key).</param>
        /// <param name="startTime">Start time of the lease.</param>
        /// <param name="endTime">End time of the lease.</param>
        /// <param name="renewTime">Time when the lease should be renewed.</param>
        public LicenseLease( string licenseString, DateTime startTime, DateTime endTime, DateTime renewTime )
        {
            this.LicenseString = licenseString ?? throw new ArgumentNullException( nameof(licenseString) );
            this.StartTime = startTime;
            this.EndTime = endTime;
            this.RenewTime = renewTime;
        }

        /// <summary>
        /// Gets the string serialization of the license (license key).
        /// </summary>
        public string? LicenseString { get; private set; }

        /// <summary>
        /// Gets the start time of the lease.
        /// </summary>
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Gets the end time of the lease.
        /// </summary>
        public DateTime EndTime { get; private set; }

        /// <summary>
        /// Gets the time from which the lease should be renewed.
        /// </summary>
        public DateTime RenewTime { get; private set; }

        /// <summary>
        /// Serializes the current <see cref="LicenseLease"/> into a string.
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            var writer = new StringBuilder();
            writer.Append( "License: " + this.LicenseString );
            writer.Append( "; StartTime: " + XmlConvert.ToString( this.StartTime, XmlDateTimeSerializationMode.Utc ) );
            writer.Append( "; EndTime: " + XmlConvert.ToString( this.EndTime, XmlDateTimeSerializationMode.Utc ) );
            writer.Append( "; RenewTime: " + XmlConvert.ToString( this.RenewTime, XmlDateTimeSerializationMode.Utc ) );

            return writer.ToString();
        }
    }
}