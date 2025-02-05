// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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