// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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