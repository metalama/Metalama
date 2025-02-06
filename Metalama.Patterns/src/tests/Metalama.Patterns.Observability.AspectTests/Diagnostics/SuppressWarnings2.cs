// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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