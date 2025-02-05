// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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