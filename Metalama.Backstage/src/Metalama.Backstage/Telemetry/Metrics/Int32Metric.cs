// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Globalization;
using System.Xml;

namespace Metalama.Backstage.Telemetry.Metrics
{
    /// <summary>
    /// A <see cref="Metric"/> storing an <see cref="int"/> value.
    /// </summary>
    [Serializable]
    public sealed class Int32Metric : Metric
    {
        public Int32Metric( string name )
            : base( name ) { }

        public Int32Metric( string name, int value )
            : base( name )
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the metric value.
        /// </summary>
        public int Value { get; set; }

        /// <summary>
        /// Increments the metric value.
        /// </summary>
        /// <param name="increment">Number of which the metric <see cref="Value"/> must be incremented.</param>
        public void Increment( int increment )
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

            int operand;

            try
            {
                operand = Convert.ToInt32( value, CultureInfo.InvariantCulture );
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