// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal static class SerializationProtocol
{
    public const int CurrentVersion = 2;
    public const int LastSupportedVersion = 2;

    /// <summary>
    /// The leading byte that marks an <em>uncompressed</em> serialized stream. A stream that does not begin with this
    /// byte is a legacy DEFLATE stream and is decompressed.
    /// </summary>
    /// <remarks>
    /// This value can never begin a valid raw DEFLATE stream (RFC 1951): the first byte's low three bits are the block
    /// header <c>BFINAL</c> + <c>BTYPE</c>, and <c>BTYPE = 0b11</c> is reserved and never emitted, so those low three
    /// bits are never <c>0b110</c> or <c>0b111</c>. <c>0xFF</c> has low three bits <c>0b111</c>, so the reader can tell
    /// the uncompressed and legacy formats apart from a single peeked byte, with no risk of collision.
    /// </remarks>
    public const byte UncompressedStreamMarker = 0xFF;
}