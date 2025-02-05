// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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