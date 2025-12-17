// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.DeclarationBuilders;

namespace Metalama.Framework.Code;

/// <summary>
/// Specifies how a declaration is implemented, indicating whether it is backed by a Roslyn symbol,
/// a pseudo member (Metalama abstraction), or introduced by an aspect.
/// </summary>
[CompileTime]
public enum DeclarationImplementationKind
{
    /// <summary>
    /// A symbol-backed declaration that exists in Roslyn. This includes both explicitly declared members
    /// and implicitly declared members like auto-property backing fields. This also includes declarations
    /// defined in source code and in referenced assemblies.
    /// </summary>
    Symbol,

    /// <summary>
    /// A declaration backed by source code (e.g., not <see cref="Introduced"/>), but that is not
    /// represented by a symbol in Roslyn. Pseudo members are Metalama abstractions such as field
    /// pseudo-accessors (getter/setter methods for fields) that don't exist as actual Roslyn symbols.
    /// </summary>
    Pseudo,

    /// <summary>
    /// A declaration introduced by an aspect through advice like <c>IntroduceMethod</c>, <c>IntroduceField</c>, etc.
    /// Not an <see cref="IDeclarationBuilder"/>, but the declaration created by the advice.
    /// </summary>
    Introduced,

    /// <summary>
    /// An <see cref="IDeclarationBuilder"/>.
    /// </summary>
    Builder,

    /// <summary>
    /// An attribute that was deserialized across projects (typically for aspect inheritance).
    /// </summary>
    DeserializedAttribute
}
