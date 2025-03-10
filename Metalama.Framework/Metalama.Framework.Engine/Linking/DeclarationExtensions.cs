// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Linking;

internal static class DeclarationExtensions
{
    public static bool IsEventFieldIntroduction( this IEventSymbol @event )
    {
        if ( @event.IsEventField() == true )
        {
            return true;
        }

        if ( @event.GetPrimaryDeclarationSyntax() is { } primaryDeclaration
             && primaryDeclaration.GetLinkerDeclarationFlags().HasFlagFast( AspectLinkerDeclarationFlags.EventField ) )
        {
            return true;
        }

        return false;
    }

    public static IFullRef<IMember> GetTypeMember( this IFullRef<IMember> member )
        => (member as IFullRef<IMethod>)?.Definition.DeclaringMember?.ToFullRef() ?? member;

    public static bool DoReturnStatementsRequireArgument( this IFullRef<IMethod> method )
        => method.Definition.ReturnType.SpecialType == Code.SpecialType.Void ||
           method.Definition.GetAsyncInfo() is { IsAsync: true, ResultType.SpecialType: Code.SpecialType.Void };
}