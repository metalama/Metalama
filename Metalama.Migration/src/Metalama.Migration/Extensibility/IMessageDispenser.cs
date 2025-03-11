// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, messages are called diagnostics and they must be defined as static fields of aspects or fabrics using the <see cref="DiagnosticDefinition{T}"/> class.
    /// </summary>
    /// <seealso href="@diagnostics"/>
    public interface IMessageDispenser
    {
        string GetMessage( string key );
    }
}