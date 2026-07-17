// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.ReflectionMocks;

namespace Metalama.Framework.Tests.UnitTests;

/// <summary>
/// Builds a <see cref="CompileTimeType"/> for a test. Note this goes through the <c>CompileTimeTypeFactory</c>, which is
/// the only thing allowed to construct a mock — tests must not bypass it, or they would not observe the caching and the
/// instance identity that production code relies on.
/// </summary>
internal static class CompileTimeTypeTestHelper
{
    public static CompileTimeType Create( IType type )
        => ((CompilationModel) type.Compilation).CompilationContext.CompileTimeTypeFactory.Get( type.GetSymbol().AssertSymbolNotNull() );
}
