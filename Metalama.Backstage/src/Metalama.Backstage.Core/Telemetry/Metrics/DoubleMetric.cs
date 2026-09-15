// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;
using System.Xml;

namespace Metalama.Backstage.Telemetry.Metrics
{
    /// <summary>
    /// A <see cref="Metric"/> storing a <see cref="double"/> value.
    /// </summary>
    [Serializable]
    public sealed class DoubleMetric : Metric
    {
        public DoubleMetric( string name ) : base( name ) { }

        public DoubleMetric( string name, double value )
            : base( name )
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the metric value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Increments the metric value.
        /// </summary>
        /// <param name="increment">Number of which the metric <see cref="Value"/> must be incremented.</param>
        public void Increment( double increment )
        {
            this.Value += increment;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return XmlConvert.ToString( this.Value );
        }

        /// <inheritdoc />
        public override bool SetValue( object? value )
        {
            if ( value == null )
            {
                return false;
            }

            double operand;

            try
            {
                operand = Convert.ToDouble( value, CultureInfo.InvariantCulture );
            }
            catch
            {
                return false;
            }

            this.Increment( operand );

            return true;
        }
    }
}