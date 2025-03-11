// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Extensions.Multicast;

[CompileTime]
internal static class MulticastTargetsHelper
{
    public static MulticastTargets GetMulticastTargets( IDeclaration declaration )
    {
        switch ( declaration.DeclarationKind )
        {
            case DeclarationKind.Compilation:
                return MulticastTargets.Assembly;

            case DeclarationKind.NamedType:
                switch ( ((INamedType) declaration).TypeKind )
                {
                    case TypeKind.Class:
                    case TypeKind.RecordClass:
                        return MulticastTargets.Class;

                    case TypeKind.Interface:
                        return MulticastTargets.Interface;

                    case TypeKind.Struct:
                    case TypeKind.RecordStruct:
                        return MulticastTargets.Struct;

                    case TypeKind.Delegate:
                        return MulticastTargets.Delegate;

                    case TypeKind.Enum:
                        return MulticastTargets.Enum;
                }

                break;

            case DeclarationKind.Finalizer:
            case DeclarationKind.Operator:
            case DeclarationKind.Method:
                return MulticastTargets.Method;

            case DeclarationKind.Property:
                return MulticastTargets.Property;

            case DeclarationKind.Indexer:
                return MulticastTargets.Property;

            case DeclarationKind.Field:
                return MulticastTargets.Field;

            case DeclarationKind.Event:
                return MulticastTargets.Event;

            case DeclarationKind.Parameter:
                return
                    ((IParameter) declaration).IsReturnParameter ? MulticastTargets.ReturnValue : MulticastTargets.Parameter;

            case DeclarationKind.Constructor:
                var isStatic = ((IConstructor) declaration).IsStatic;

                return
                    isStatic ? MulticastTargets.StaticConstructor : MulticastTargets.InstanceConstructor;
        }

        return MulticastTargets.Default;
    }
}