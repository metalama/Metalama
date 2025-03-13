// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.SuppressWarnings2;

[Observable]
public class Vector
{
    public double X { get; set; }

    public double Y { get; set; }

    public double NormWithWarning => VectorHelper.ComputeNorm1( this );

    [SuppressObservabilityWarnings]
    public double NormWithoutWarning => VectorHelper.ComputeNorm2( this );
}

public static class VectorHelper
{
    public static double ComputeNorm1( Vector v ) => Math.Sqrt( (v.X * v.X) + (v.Y * v.Y) );

    public static double ComputeNorm2( Vector v ) => Math.Sqrt( (v.X * v.X) + (v.Y * v.Y) );
}