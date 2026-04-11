// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.RunTime;

/// <summary>
/// A custom attribute added by the framework to a source-compatibility <em>forwarding constructor</em>: a compile-time stub that keeps
/// the pre-mutation signature of an aspect-mutated constructor callable so that existing call sites continue to
/// compile and existing assemblies that link against the type continue to bind. The source-compatibility forwarding constructor preserves
/// both source and binary compatibility with the source constructor; it chains, via <c>: this(...)</c>,
/// to the constructor that <see cref="Metalama.Framework.Advising.IAdviceFactory"/> has mutated with an appended parameter.
/// </summary>
/// <remarks>Aspect authors should not add this attribute directly. Use
/// <see cref="Metalama.Framework.Code.ConstructorExtensions.IsSourceCompatibilityConstructor"/> from a custom
/// <see cref="Metalama.Framework.Advising.IPullStrategy"/> to detect such constructors.</remarks>
[AttributeUsage( AttributeTargets.Constructor )]
public sealed class SourceCompatibilityConstructorAttribute : Attribute;
