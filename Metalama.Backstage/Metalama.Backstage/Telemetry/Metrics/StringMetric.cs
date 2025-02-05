// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Telemetry.Metrics
{
    /// <summary>
    /// A <see cref="Metric"/> storing a <see cref="string"/> value.
    /// </summary>
    [Serializable]
    public sealed class StringMetric : Metric
    {
        public StringMetric( string name ) : base( name ) { }

        public StringMetric( string name, string? value )
            : base( name )
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the metric value.
        /// </summary>
        public string? Value { get; set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Value ?? "null";
        }

        /// <inheritdoc />
        public override bool SetValue( object? value )
        {
            throw new NotSupportedException();
        }
    }
}