// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Transformations;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.Linking;

internal record struct LinkerInjectedInterface( IInjectInterfaceTransformation Transformation, BaseTypeSyntax Syntax );