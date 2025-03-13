// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace PostSharp.Extensibility
{
    /// <summary>
    /// In Metalama, use the same mechanism as for suppressing C# or analyzer warnings.
    /// </summary>
    [AttributeUsage( AttributeTargets.All, AllowMultiple = true, Inherited = false )]
    public class SuppressWarningAttribute : Attribute
    {
        public SuppressWarningAttribute( string messageId )
        {
            this.MessageId = messageId;
        }

        public string MessageId { get; }

        public string Reason { get; set; }
    }
}