// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Assets
{
    // Resharper disable ClassNeverInstantiated.Global
    // Resharper disable UnusedMember.Global

    internal class Box<T>
    {
        public T? Value { get; set; }

        public class InnerBox
        {
            public enum Shiny
            {
                Yes,
                No
            }
        }

        [Flags]
        public enum Color
        {
            Blue = 4,
            Red = 8
        }
    }
}