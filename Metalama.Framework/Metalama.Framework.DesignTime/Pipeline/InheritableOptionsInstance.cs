// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Options;

namespace Metalama.Framework.DesignTime.Pipeline;

internal sealed record InheritableOptionsInstance( HierarchicalOptionsKey Key, IHierarchicalOptions Options );