// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Introspection;

namespace Metalama.Framework.Engine.Introspection;

public interface IIntrospectionOptionsProvider
{
    IntrospectionOptions IntrospectionOptions { get; }
}