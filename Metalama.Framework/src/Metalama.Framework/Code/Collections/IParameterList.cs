// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections
{
    /// <summary>
    /// Read-only list of <see cref="IParameter"/>.
    /// </summary>
    [CompileTime]
    public interface IParameterList : IReadOnlyList<IParameter>
    {
        IParameter this[ string name ] { get; }

        dynamic ToValueArray();
    }
}