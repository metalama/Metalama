// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Utilities;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;

namespace Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;

// ReSharper disable once RedundantTypeDeclarationBody
[Durable]
[CompileTime]
[ImmutableType]
internal interface ICommandNamingConvention : INamingConvention<IMethod, CommandNamingConventionMatch> { }