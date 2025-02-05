// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.CodeModel.Abstractions;

internal enum DeclarationImplementationKind
{
    /// <summary>
    /// A symbol-backed declaration.
    /// </summary>
    Symbol,

    /// <summary>
    /// A declaration backed by source code (e.g., not <see cref="Introduced"/>), but that is not
    /// represented by a symbol in Roslyn.
    /// </summary>
    Pseudo,

    /// <summary>
    /// A declaration introduced by an aspect.
    /// </summary>
    Introduced,
    
    Builder,
    DeserializedAttribute
}