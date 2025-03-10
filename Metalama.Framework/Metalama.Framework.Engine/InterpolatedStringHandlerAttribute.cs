// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if NETSTANDARD2_0 // ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices;

[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct, Inherited = false )]
internal sealed class InterpolatedStringHandlerAttribute : Attribute;
#endif