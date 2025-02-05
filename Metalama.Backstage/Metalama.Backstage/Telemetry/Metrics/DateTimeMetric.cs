// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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