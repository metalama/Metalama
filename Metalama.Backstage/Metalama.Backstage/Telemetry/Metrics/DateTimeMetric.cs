// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Xml;

namespace Metalama.Backstage.Telemetry.Metrics
{
    /// <summary>
    /// A <see cref="Metric"/> storing a <see cref="DateTime"/> value.
    /// </summary>
    [Serializable]
    public class DateTimeMetric : Metric
    {
        public DateTimeMetric( string name ) : base( name ) { }

        public DateTimeMetric( string name, DateTime value )
            : base( name )
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the metric value.
        /// </summary>
        public DateTime Value { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return XmlConvert.ToString( this.Value, XmlDateTimeSerializationMode.RoundtripKind );
        }

        public override bool SetValue( object? value )
        {
            throw new NotSupportedException();
        }
    }
}