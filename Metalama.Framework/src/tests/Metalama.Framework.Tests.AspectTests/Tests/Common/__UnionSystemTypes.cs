// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// The two types that the compiler requires of a union declaration. No target framework declares them yet, so the
// compiler reports CS0518 for the interface and CS0656 for the constructor of the attribute when they are absent.
//
// PostSharp.Engineering ships the same two types as system type polyfills from 2023.2.450, and this repository stays
// on 2023.2.448 for the reason that Directory.Packages.props gives. That version would not remove this file in any
// case. A test of the aspect test suite is not compiled by the test project: the test runner builds a compilation of
// its own from the test source files, and a polyfill that the package adds to a project as an internal type is not
// visible to it. This file is the copy that the test compilation receives, through the IncludedFiles option of the
// metalamaTests.json of the union tests, so that the declaration is written once rather than in each test.
//
// The file name begins with two underscores, which excludes it from the compilation of the test project itself. Once
// this repository consumes a version that carries the polyfills, that project declares the two types as well, and a
// second declaration of the same type in the same namespace is CS0101.

// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

/// <summary>
/// Provides a common interface for accessing the contents of a union type at run time.
/// </summary>
internal interface IUnion
{
    /// <summary>
    /// Gets the value contained in the union, or <see langword="null"/> if the union has no value.
    /// </summary>
    object? Value { get; }
}

/// <summary>
/// Indicates that a class or struct is a union type, enabling compiler support for union behaviors.
/// </summary>
[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false )]
internal sealed class UnionAttribute : Attribute { }
