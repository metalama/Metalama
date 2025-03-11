// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// No equivalent in Metalama.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class, AllowMultiple = true )]
    [PublicAPI]
    public class MetricAttribute : Attribute
    {
        public string MetricName { get; }

        public object Operand { get; }

        public MetricAttribute( string metricName, object operand )
        {
            throw new NotImplementedException();
        }

        public bool AllowImplementationMetrics { get; set; }
    }
}