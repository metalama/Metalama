// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Utilities;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Exposes a <see cref="Compilation"/> property.
    /// </summary>
    [InternalImplement]
    [CompileTime]
    [Hidden]
    public interface ICompilationElement
    {
        /// <summary>
        /// Gets the <see cref="ICompilation"/> to which this type belongs (which does not mean that the type is declared
        /// by the main project of the compilation).
        /// </summary>
        ICompilation Compilation { get; }

        /// <summary>
        /// Gets the kind of declaration.
        /// </summary>
        DeclarationKind DeclarationKind { get; }
    }
}