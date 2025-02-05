// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Globalization;
using System.Xml;

namespace Metalama.Backstage.Telemetry.Metrics
{
    /// <summary>
    /// A <see cref="Metric"/> storing a <see cref="bool"/> value.
    /// </summary>
    [Serializable]
    public sealed class BoolMetric : Metric
    {
        public BoolMetric( string name )
            : base( name ) { }

        public BoolMetric( string name, bool value )
            : base( name )
        {
            this.Value = value;
        }

        public bool Value { get; set; }

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

            bool operand;

            try
            {
                operand = Convert.ToBoolean( value, CultureInfo.InvariantCulture );
            }
            catch
            {
                return false;
            }

            this.Value |= operand;

            return true;
        }
    }
}