// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.LinkerTests.Tests
{
    public static class Api
    {
        public const string Inline = "Inline";
        public const string Base = "Base";
        public const string Previous = "Previous";
        public const string Current = "Current";
        public const string Final = "Final";

        public static dynamic This = new object();
        public static dynamic Static = new object();
        public static dynamic Local = new object();

        public static dynamic Link { get; set; } = new object();

        public static T Cast<T>(object o) => (T)o;
    }
}
