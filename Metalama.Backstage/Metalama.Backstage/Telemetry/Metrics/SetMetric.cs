// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;

namespace Metalama.Backstage.Telemetry.Metrics
{
    /// <summary>
    /// A <see cref="Metric"/> storing a set of strings.
    /// </summary>
    [Serializable]
    public sealed class SetMetric : Metric
    {
        private readonly HashSet<string> _set = [];

        public SetMetric( string name ) : base( name ) { }

        [PublicAPI]
        public HashSet<string> Set => this._set;

        /// <inheritdoc />
        public override void WriteValue( TextWriter textWriter )
        {
            var first = true;

            foreach ( var s in this._set )
            {
                if ( first )
                {
                    first = false;
                }
                else
                {
                    textWriter.Write( ',' );
                }

                textWriter.Write( s );
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            using var stringWriter = new StringWriter();
            this.WriteValue( stringWriter );

            return stringWriter.ToString();
        }

        /// <inheritdoc />
        public override bool SetValue( object? value )
        {
            if ( value is not string operand )
            {
                return false;
            }

            this._set.Add( operand );

            return true;
        }
    }
}