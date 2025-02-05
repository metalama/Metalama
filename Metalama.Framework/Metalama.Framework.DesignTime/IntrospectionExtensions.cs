// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Introspection;

namespace Metalama.Framework.DesignTime;

internal static class IntrospectionExtensions
{
    public static bool ShouldBeDisplayed( this IIntrospectionTransformation transformation )
        => transformation.TransformationKind != IntrospectionTransformationKind.AddAnnotation;
}