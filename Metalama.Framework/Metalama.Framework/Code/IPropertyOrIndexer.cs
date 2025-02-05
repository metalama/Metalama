// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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