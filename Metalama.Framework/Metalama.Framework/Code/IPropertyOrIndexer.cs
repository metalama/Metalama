// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Reflection;

namespace Metalama.Framework.Code;

public interface IPropertyOrIndexer : IFieldOrPropertyOrIndexer
{
    /// <summary>
    /// Gets a <see cref="PropertyInfo"/> that represents the current property at run time.
    /// </summary>
    /// <returns>A <see cref="PropertyInfo"/> that can be used only in run-time code.</returns>
    [CompileTimeReturningRunTime]
    PropertyInfo ToPropertyInfo();

    new IRef<IPropertyOrIndexer> ToRef();
}