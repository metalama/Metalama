// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// Extension methods for <see cref="RefTargetKind"/>.
/// </summary>
internal static class RefTargetKindExtensions
{
    public static RefTargetKind ToDeclarationRefTargetKind(
        this MethodKind methodKind,
        RefTargetKind existingTargetKind = RefTargetKind.Default )
        => (methodKind, existingTargetKind) switch
        {
            (MethodKind.PropertyGet, RefTargetKind.Default) => RefTargetKind.PropertyGet,
            (MethodKind.PropertySet, RefTargetKind.Default) => RefTargetKind.PropertySet,
            (MethodKind.EventRaise, RefTargetKind.Default) => RefTargetKind.EventRaise,
            (MethodKind.PropertySet, RefTargetKind.Parameter) => RefTargetKind.PropertySetParameter,
            (MethodKind.EventRaise, RefTargetKind.Parameter) => RefTargetKind.EventRaiseParameter,
            (MethodKind.PropertyGet, RefTargetKind.Return) => RefTargetKind.PropertyGetReturnParameter,
            (MethodKind.PropertySet, RefTargetKind.Return) => RefTargetKind.PropertySetReturnParameter,
            (MethodKind.EventRaise, RefTargetKind.Return) => RefTargetKind.EventRaiseReturnParameter,
            _ => throw new InvalidOperationException( $"Unexpected combination: '{methodKind}', '{existingTargetKind}'." )
        };

    public static DeclarationKind? ToDeclarationKind( this RefTargetKind kind )
        => kind switch
        {
            RefTargetKind.Default => null,
            RefTargetKind.Return => DeclarationKind.Parameter,
            RefTargetKind.Assembly => DeclarationKind.AssemblyReference,
            RefTargetKind.Module => DeclarationKind.Compilation,
            RefTargetKind.Field => DeclarationKind.Field,
            RefTargetKind.Parameter => DeclarationKind.Parameter,
            RefTargetKind.Property => DeclarationKind.Property,
            RefTargetKind.Event => DeclarationKind.Event,
            RefTargetKind.PropertyGet => DeclarationKind.Method,
            RefTargetKind.PropertySet => DeclarationKind.Method,
            RefTargetKind.StaticConstructor => DeclarationKind.Constructor,
            RefTargetKind.PropertySetParameter => DeclarationKind.Parameter,
            RefTargetKind.PropertyGetReturnParameter => DeclarationKind.Parameter,
            RefTargetKind.PropertySetReturnParameter => DeclarationKind.Parameter,
            RefTargetKind.EventRaise => DeclarationKind.Method,
            RefTargetKind.EventRaiseParameter => DeclarationKind.Parameter,
            RefTargetKind.EventRaiseReturnParameter => DeclarationKind.Parameter,
            RefTargetKind.NamedType => DeclarationKind.NamedType,
            _ => throw new ArgumentOutOfRangeException( nameof(kind), kind, null )
        };
}