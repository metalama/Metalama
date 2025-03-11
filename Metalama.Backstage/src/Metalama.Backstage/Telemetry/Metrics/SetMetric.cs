// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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