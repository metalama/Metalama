// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace PostSharp.Reflection
{
    /// <summary>
    /// See individual methods for migration assistance.
    /// </summary>
    public static class ReflectionSearch
    {
        /// <summary>
        /// This is currently not exposed in Metalama but it is implemented internally.
        /// </summary>
        public static CustomAttributeInstance[] GetCustomAttributesOfType( Type customAttributeType )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is currently not exposed in Metalama but it is implemented internally.
        /// </summary>
        public static CustomAttributeInstance[] GetCustomAttributesOfType( Type customAttributeType, ReflectionSearchOptions options )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="IDeclaration"/>.<see cref="IDeclaration.Attributes"/>.
        /// </summary>
        public static CustomAttributeInstance[] GetCustomAttributesOnTarget( object target )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="IDeclaration"/>.<see cref="IDeclaration.Attributes"/>.
        /// </summary>
        public static CustomAttributeInstance[] GetCustomAttributesOnTarget( object target, ReflectionSearchOptions options )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="IDeclaration"/>.<see cref="IDeclaration.Attributes"/>.
        /// </summary>
        public static IList<T> GetCustomAttributesOnTarget<T>( object target, ReflectionSearchOptions options = ReflectionSearchOptions.IncludeDerivedTypes )
            where T : Attribute
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="IDeclaration"/>.<see cref="IDeclaration.Attributes"/>.
        /// </summary>
        public static bool HasCustomAttribute( object target, Type type, bool inherit = false )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, the feature is not exposed on the code model, but it is a part of the validation feature thanks to the
        /// <c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions.ValidateInboundReferences</c> method.
        /// </summary>
        /// <seealso href="@aspect-validating"/>
        /// <seealso href="@validating-usage"/>
        public static MethodUsageCodeReference[] GetDeclarationsUsedByMethod( MethodBase method )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, the feature is not exposed on the code model, but it is a part of the validation feature thanks to the
        /// <c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions.ValidateInboundReferences</c> method.
        /// </summary>
        /// <seealso href="@aspect-validating"/>
        /// <seealso href="@validating-usage"/>
        public static MethodUsageCodeReference[] GetDeclarationsUsedByMethod( MethodBase method, ReflectionSearchOptions options )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, the feature is not exposed on the code model, but it is a part of the validation feature thanks to the
        /// <c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions.ValidateInboundReferences</c> method.
        /// </summary>
        /// <seealso href="@validating-usage"/>
        public static MethodUsageCodeReference[] GetMethodsUsingDeclaration( MemberInfo declaration )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, the feature is not exposed on the code model, but it is a part of the validation feature thanks to the
        /// <c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions.ValidateInboundReferences</c> method.
        /// </summary>
        /// <seealso href="@validating-usage"/>
        public static MethodUsageCodeReference[] GetMethodsUsingDeclaration( MemberInfo declaration, ReflectionSearchOptions options )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="ICompilation"/>.<see cref="ICompilation.GetDerivedTypes(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.DerivedTypesOptions)"/>.
        /// </summary>
        public static TypeInheritanceCodeReference[] GetDerivedTypes( Type baseType )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, use <see cref="ICompilation"/>.<see cref="ICompilation.GetDerivedTypes(Metalama.Framework.Code.INamedType,Metalama.Framework.Code.DerivedTypesOptions)"/>.
        /// </summary>
        public static TypeInheritanceCodeReference[] GetDerivedTypes( Type baseType, ReflectionSearchOptions options )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, the feature is not exposed on the code model, but it is a part of the validation feature thanks to the
        /// <c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions.ValidateInboundReferences</c> method.
        /// </summary>
        /// <seealso href="@validating-usage"/>
        public static MemberTypeCodeReference[] GetMembersOfType( Type memberType )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// In Metalama, the feature is not exposed on the code model, but it is a part of the validation feature thanks to the
        /// <c>Metalama.Extensions.Validation.ReferenceValidationQueryExtensions.ValidateInboundReferences</c> method.
        /// </summary>
        /// <seealso href="@validating-usage"/>
        public static MemberTypeCodeReference[] GetMembersOfType( Type memberType, ReflectionSearchOptions options )
        {
            throw new NotImplementedException();
        }
    }
}