// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.LinkerTests.Tests
{
    public static class Api
    {
        public const string inline = "inline";
        public const string @base = "base";
        public const string previous = "previous";
        public const string current = "current";
        public const string final = "final";

        public static dynamic _this = new object();
        public static dynamic _static = new object();
        public static dynamic _local = new object();

        public static dynamic link { get; set; } = new object();

        public static T _cast<T>(object o) => (T)o;
    }
}
