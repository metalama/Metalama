// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @IncludePolyfill(IUnion,UnionAttribute)
#endif

// Covers the first acceptance criterion of issue #1946: a property whose type is a union declaration produces no
// observability warning when a computed property reads through it.
//
// The dependency graph classifies such a property as the stem of a chain and reports LAMA5161 when the type of the
// stem is neither observable nor immutable. A union declaration is immutable, because its only instance field is the
// read-only backing field of the synthesized get-only Value property, so the immutability library classifies it as
// shallowly immutable and the warning is not reported. Before that classification existed, the warning was reported
// for every union that was not declared readonly.
//
// The union-typed property is get-only, which is a workaround rather than a part of what the test asserts. The setter
// that the library introduces for a property of a value type compares with the equality operator, a union declaration
// synthesizes no such operator, and the generated code then does not compile. That defect is not specific to unions
// and is filed as issue #2030.

namespace Metalama.Patterns.Observability.AspectTests.UnionPropertyIsNotWarned;

public union Result( int, string );

// <target>
[Observable]
public class C
{
    public Result CurrentResult { get; }

    public object? CurrentValue => this.CurrentResult.Value;
}
