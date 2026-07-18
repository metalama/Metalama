// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.ReflectionMocks;
using System;

namespace Metalama.Framework.Tests.UnitTests;

/// <summary>
/// Builds a <see cref="CompileTimeType"/> for a test. This goes through the <c>CompileTimeTypeFactory</c>, which is the
/// only thing allowed to construct a mock: tests must not bypass it, or they would not observe the caching and the
/// instance identity that production code relies on.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IType.ToType"/> is deliberately <em>not</em> used here, even though it is the natural-looking choice. It
/// resolves through <c>SystemTypeResolver</c>, which derives from <c>CurrentAppDomainTypeResolver</c> and returns the
/// real <see cref="Type"/> whenever the type can be loaded into the current <c>AppDomain</c>, falling back to a mock
/// only when it cannot. For the types these tests use, mostly framework types such as <see cref="int"/> and
/// <see cref="string"/>, it would therefore hand back a genuine CLR type.
/// </para>
/// <para>
/// That is the right behaviour for production code, which wants a usable type, but the wrong one for tests whose whole
/// subject is the mock: they would silently end up asserting on the CLR's implementation instead of ours.
/// </para>
/// </remarks>
internal static class CompileTimeTypeTestHelper
{
    public static CompileTimeType Create( IType type )
        => ((CompilationModel) type.Compilation).CompilationContext.CompileTimeTypeFactory.Get( type.GetSymbol().AssertSymbolNotNull() );
}
