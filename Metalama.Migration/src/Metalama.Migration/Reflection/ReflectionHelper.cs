// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Reflection;

namespace PostSharp.Reflection
{
    /// <summary>
    /// The equivalent in Metalama is <see cref="Metalama.Framework.RunTime.ReflectionHelper"/> but it does not cover the same functions.
    /// </summary>
    public static class ReflectionHelper
    {
        public static PropertyInfo GetProperty( Type declaringType, MethodInfo getter, MethodInfo setter )
        {
            throw new NotImplementedException();
        }

        public static PropertyInfo GetProperty( Type declaringType, MethodInfo getter, MethodInfo setter, bool throwOnMissingMember )
        {
            throw new NotImplementedException();
        }

        public static EventInfo GetEvent( Type declaringType, MethodInfo addMethod, MethodInfo removeMethod, MethodInfo raiseMethod )
        {
            throw new NotImplementedException();
        }

        public static LocationInfo GetLocation( Type declaringType, MethodInfo getter, MethodInfo setter )
        {
            throw new NotImplementedException();
        }

        public static bool IsAvailableInTargetFramework( this MemberInfo memberInfo )
        {
            throw new NotImplementedException();
        }

        public static bool IsAvailableInTargetFramework( this Type type )
        {
            throw new NotImplementedException();
        }

        public static bool IsCompilerGenerated( this MemberInfo member )
        {
            throw new NotImplementedException();
        }

        public static bool IsCompilerGenerated( this Type type )
        {
            throw new NotImplementedException();
        }

        public static SemanticInfo GetSemanticInfo( this MemberInfo member )
        {
            throw new NotImplementedException();
        }

        public static SemanticInfo GetSemanticInfo( this Type type )
        {
            throw new NotImplementedException();
        }

        public static bool AreInternalsVisibleToCurrentProject( this Assembly definingAssembly )
        {
            throw new NotImplementedException();
        }

        public static bool AreInternalsVisibleTo( this Assembly definingAssembly, Assembly referencingAssembly )
        {
            throw new NotImplementedException();
        }

        public static string GetAssemblyQualifiedTypeName( string typeName, string assemblyName )
        {
            throw new NotImplementedException();
        }

        public static void ParseAssemblyQualifiedTypeName( string assemblyQualifiedTypeName, out string typeName, out string assemblyName )
        {
            throw new NotImplementedException();
        }
    }
}