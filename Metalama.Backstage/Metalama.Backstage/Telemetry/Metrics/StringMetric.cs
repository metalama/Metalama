// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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