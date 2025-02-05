// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Introspection;
using Metalama.Framework.Introspection;

namespace Metalama.Framework.Workspaces;

internal sealed class IntrospectionOptionsBox : IIntrospectionOptionsProvider
{
    public IntrospectionOptions IntrospectionOptions { get; set; } = new();
}