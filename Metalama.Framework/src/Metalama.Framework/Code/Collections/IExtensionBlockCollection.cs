// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.Code.Collections;

/// <summary>
/// Represents a list of extension blocks.
/// </summary>
public interface IExtensionBlockCollection : IReadOnlyCollection<IExtensionBlock>
{
    /// <summary>
    /// Gets the list of extension blocks extending a specific type (i.e. matching a specific <see cref="IExtensionBlock.ReceiverType"/>), given as an <see cref="IType"/>.
    /// </summary>
    IEnumerable<IExtensionBlock> OfReceivingType( IType type );

    /// <summary>
    /// Gets the list of extension blocks extending a specific type (i.e. matching a specific <see cref="IExtensionBlock.ReceiverType"/>),, given as a <see cref="Type"/>.
    /// </summary>
    IEnumerable<IExtensionBlock> OfReceivingType( Type type );
}