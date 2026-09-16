// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @LanguageVersion(15.0)
// @IncludePolyfill(IUnion,UnionAttribute)
// @RemoveOutputCode
#endif

// Pins the behaviour of finding UT-14a of issue #1946: the Observability library rejects a union declaration, and it
// reports two diagnostics rather than one.
//
// The C# compiler reports CS0592 because ObservableAttribute declares AttributeTargets.Class and
// AttributeTargets.Interface, and a union declaration is a struct. Roslyn nevertheless keeps the bound attribute, and
// Metalama discovers attributes from syntax, so the aspect instance is created and the eligibility rule of the aspect,
// which requires a class, reports LAMA0037 as well. Only LAMA0037 appears in the expected output, because the test
// framework reports the diagnostics of the pipeline and not those of the input compilation. CS0592 is recorded
// instead by the Compile Remove item of the project file, which exists because this file does not compile.
//
// The behaviour is intended and is not changed by this test. A union declaration carries no observable state: its only
// synthesized state is the get-only Value property, which the compiler forbids the user to add to.
//
// The justification of LAMA0037 is empty, which is a defect of the engine and not of this library, and it is pinned
// here as it is. ObservableAttribute is inheritable, so the aspect source asks for the justification of the
// Inheritance scenario, while the rule that rejects a type that is not a class is declared under
// ExceptForInheritance and therefore leaves that scenario eligible. No rule then answers for the requested scenario
// and the justification is null. The same message appears for an ordinary struct, so the defect is reachable without
// C# 15. It is filed as issue #2029.

namespace Metalama.Patterns.Observability.AspectTests.Diagnostics.UnionIsNotObservable;

public record Success( int Value );

public record Failure( string Message );

// <target>
[Observable]
public union Result( Success, Failure );
