// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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