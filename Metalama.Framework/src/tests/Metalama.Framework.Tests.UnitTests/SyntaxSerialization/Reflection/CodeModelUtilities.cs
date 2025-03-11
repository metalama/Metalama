// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System.Linq;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    internal static class CodeModelUtilities
    {
        public static IMethod Method( this INamedType type, string name ) => type.Methods.Single( m => m.Name == name );

        public static IProperty Property( this INamedType type, string name ) => type.Properties.OfName( name ).Single();

        public static IField Field( this INamedType type, string name ) => type.Fields.OfName( name ).Single();

        public static IEvent Event( this INamedType type, string name ) => type.Events.OfName( name ).Single();
    }
}