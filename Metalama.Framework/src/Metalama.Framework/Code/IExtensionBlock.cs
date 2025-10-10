// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Code;

/// <summary>
/// Represents an extension block.
/// </summary>
/// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-14.0/extensions"/>
public interface IExtensionBlock : INamedType
{
    /// <summary>
    /// Gets the receiver type, i.e. the type "extended" by the current block, i.e. also the type of the <see cref="ReceiverParameter"/>.
    /// </summary>
    IType ReceiverType { get; }

    /// <summary>
    /// Gets the receiver parameter, i.e. the parameter of the <c>extension</c> keyword.
    /// </summary>
    IParameter ReceiverParameter { get; }
    
    new IRef<IExtensionBlock> ToRef();
    
    /// <summary>
    /// Gets the type containing the current extension block.
    /// </summary>
    new INamedType DeclaringType { get; }
}