// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Issue1722.Primitives;

// A compile-time-only utility class in its OWN package/assembly. It contains NO aspect, template, fabric or
// option types, so its compile-time assembly (ml!Issue1722.Primitives) is only ever brought into the
// CompileTimeDomain through the recursive reference preload of the consuming aspect's compile-time project.
[CompileTime]
public static class AspectUtilities
{
    // The extension method reported in issue #1722. Calling it via extension syntax
    // (method.ReturnType.IsResultTask()) from another package's BuildEligibility triggers a
    // FileNotFoundException for 'ml!Issue1722.Primitives...'; calling it as a plain static method works.
    public static bool IsResultTask( this IType type )
        => type.IsConvertibleTo( typeof(Task<>), ConversionKind.TypeDefinition );
}
