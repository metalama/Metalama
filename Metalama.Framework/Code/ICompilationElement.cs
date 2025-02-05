// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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