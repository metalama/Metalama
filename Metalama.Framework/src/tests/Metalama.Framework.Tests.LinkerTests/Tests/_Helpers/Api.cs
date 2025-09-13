// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.LinkerTests.Tests
{
    public static class Api
    {
        public const string Inline = "inline";
        public const string Base = "base";
        public const string Previous = "previous";
        public const string Current = "current";
        public const string Final = "final";

        public static dynamic This = new object();
        public static dynamic Static = new object();
        public static dynamic Local = new object();

        public static dynamic Link { get; set; } = new object();

        public static T Cast<T>(object o) => (T)o;
    }
}
