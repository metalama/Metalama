// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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