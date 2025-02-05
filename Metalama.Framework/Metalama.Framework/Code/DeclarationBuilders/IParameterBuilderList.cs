// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;

namespace Metalama.Framework.Code.DeclarationBuilders
{
    /// <summary>
    /// Read-only list of <see cref="IParameterBuilder"/>.
    /// </summary>
    public interface IParameterBuilderList : IReadOnlyList<IParameterBuilder>
    {
        // TODO: This type cannot simply extend IParameterList, because it leads to ambiguity of indexer, GetEnumerator etc.
        // The only way to do this is to redeclare all IReadOnlyList members here to hide conflicting base interface members.

        IParameterBuilder this[ string name ] { get; }
    }
}