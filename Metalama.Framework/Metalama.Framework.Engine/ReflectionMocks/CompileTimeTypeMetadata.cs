// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Framework.Engine.ReflectionMocks;

internal record struct CompileTimeTypeMetadata( string? Namespace, string Name, string FullName, string ToStringName );