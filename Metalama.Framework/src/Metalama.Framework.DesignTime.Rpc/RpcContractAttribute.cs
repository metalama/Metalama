// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.Rpc;

/// <summary>
/// Marks a type as an RPC contract that is serialized using MessagePack during RPC communication.
/// This is a marker attribute used for documentation purposes only and has no runtime effect.
/// </summary>
[AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct, Inherited = false )]
public sealed class RpcContractAttribute : Attribute;
