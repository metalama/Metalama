// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Reflection;

namespace PostSharp.Reflection
{
    /// <summary>
    /// There is no direct equivalent in Metalama, but individual methods may have. 
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// In Metalama, backing field of automatic properties are not exposed to the code model.
        /// </summary>
        public static PropertyInfo GetAutomaticProperty( this FieldInfo field )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, backing field of automatic properties are not exposed to the code model.
        /// </summary>
        public static PropertyInfo GetAutomaticProperty( this FieldInfo field, bool inherit )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="IFieldOrProperty.IsAutoPropertyOrField"/>.
        /// </summary>
        public static bool IsAutomaticProperty( this PropertyInfo propertyInfo )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not supported in Metalama.
        /// </summary>
        public static FieldInfo GetBackingField( this PropertyInfo propertyInfo )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="IMethod"/>.<see cref="MethodExtensions.GetAsyncInfo"/> at compile time. There is no equivalent at run time.
        /// </summary>
        public static StateMachineKind GetStateMachineKind( this MethodInfo method )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="IMethod"/>.<see cref="MethodExtensions.GetAsyncInfo"/> at compile time. There is no equivalent at run time.
        /// </summary>
        public static MethodInfo GetStateMachinePublicMethod( this MethodInfo method )
        {
            throw new NotImplementedException();
        }
    }
}