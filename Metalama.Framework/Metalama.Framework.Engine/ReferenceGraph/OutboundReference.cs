// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.ReferenceGraph;

internal sealed record OutboundReference( ISymbol ReferencedSymbol, ISymbol ReferencingSymbol, SyntaxNodeOrToken Node, ReferenceKinds ReferenceKind );