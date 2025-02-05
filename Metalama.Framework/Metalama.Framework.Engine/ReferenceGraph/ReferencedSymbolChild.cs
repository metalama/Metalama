// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Validation;

namespace Metalama.Framework.Engine.ReferenceGraph;

internal readonly record struct ReferencedSymbolChild( ReferencedSymbolInfo Info, ChildKinds Kind );