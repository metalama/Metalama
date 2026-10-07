// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Aspects
{
    /// <summary>
    /// Marks an object that a Metalama extension makes available to a template through <see cref="meta.GetExtension{T}"/> and
    /// <see cref="meta.TryGetExtension{T}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An extension, for instance the interceptors of Metalama Premium, adds information that the standard <c>meta</c> API does not provide. The
    /// information applies to the declaration that the template implements. A template that is called by another template sees the extensions of
    /// the calling template.
    /// </para>
    /// <para>
    /// An extension typically exposes its object through a C# 14 extension property of <see cref="meta"/>, so that the template reads it as an
    /// ordinary member of <c>meta</c>.
    /// </para>
    /// </remarks>
    [CompileTime]
    public interface IMetaExtension;
}
