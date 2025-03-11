// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;

namespace Metalama.Patterns.Wpf.Implementation.CommandNamingConvention;

[CompileTime]
internal static class CommandNamingConventionMatcher
{
    public static CommandNamingConventionMatch Match(
        INamingConvention namingConvention,
        IMethod executeMethod,
        string commandPropertyName,
        INameMatchPredicate matchCanExecuteNamePredicate,
        bool considerMethod = true,
        bool considerProperty = true,
        bool requireCanExecuteMatch = false )
    {
        var inspectedMembers = new List<InspectedMember>();

        var declaringType = executeMethod.DeclaringType;

        var conflictingMember = (IMemberOrNamedType?) declaringType.AllMembers().FirstOrDefault( m => m.Name == commandPropertyName )
                                ?? declaringType.Types.FirstOrDefault( t => t.Name == commandPropertyName );

        var commandPropertyConflictMatch =
            conflictingMember != null
                ? MemberMatch<IMemberOrNamedType, DefaultMatchKind>.Conflict( conflictingMember )
                : MemberMatch<IMemberOrNamedType, DefaultMatchKind>.Success( DefaultMatchKind.Default );

        MemberMatch<IMethod, DefaultMatchKind>? canExecuteMethodMatch = null;

        if ( considerMethod )
        {
            canExecuteMethodMatch =
                executeMethod.DeclaringType.Methods.FindMatchingMembers(
                    matchCanExecuteNamePredicate,
                    IsValidCanExecuteMethod,
                    inspectedMembers.Add,
                    CommandAttribute.CanExecuteMethodCategory );
        }

        MemberMatch<IProperty, DefaultMatchKind>? canExecutePropertyMatch = null;

        if ( considerProperty )
        {
            canExecutePropertyMatch =
                executeMethod.DeclaringType.Properties.FindMatchingMembers(
                    matchCanExecuteNamePredicate,
                    IsValidCanExecuteProperty,
                    inspectedMembers.Add,
                    CommandAttribute.CanExecutePropertyCategory );
        }

        switch ( canExecuteMethodMatch?.Outcome )
        {
            case MemberMatchOutcome.Success when canExecutePropertyMatch?.Outcome == MemberMatchOutcome.Success:
                return new CommandNamingConventionMatch(
                    namingConvention,
                    commandPropertyName,
                    commandPropertyConflictMatch,
                    MemberMatch<IMember, DefaultMatchKind>.Ambiguous(),
                    inspectedMembers,
                    requireCanExecuteMatch );

            case MemberMatchOutcome.Success:
                return new CommandNamingConventionMatch(
                    namingConvention,
                    commandPropertyName,
                    commandPropertyConflictMatch,
                    canExecuteMethodMatch.Cast<IMember>(),
                    inspectedMembers,
                    requireCanExecuteMatch );

            default:
                {
                    if ( canExecutePropertyMatch?.Outcome == MemberMatchOutcome.Success )
                    {
                        return new CommandNamingConventionMatch(
                            namingConvention,
                            commandPropertyName,
                            commandPropertyConflictMatch,
                            canExecutePropertyMatch.Cast<IMember>(),
                            inspectedMembers,
                            requireCanExecuteMatch );
                    }
                    else if ( canExecuteMethodMatch?.Outcome == MemberMatchOutcome.Ambiguous
                              || canExecutePropertyMatch?.Outcome == MemberMatchOutcome.Ambiguous )
                    {
                        return new CommandNamingConventionMatch(
                            namingConvention,
                            commandPropertyName,
                            commandPropertyConflictMatch,
                            MemberMatch<IMember, DefaultMatchKind>.Ambiguous(),
                            inspectedMembers,
                            requireCanExecuteMatch );
                    }
                    else if ( canExecuteMethodMatch?.Outcome == MemberMatchOutcome.Invalid || canExecutePropertyMatch?.Outcome == MemberMatchOutcome.Invalid )
                    {
                        return new CommandNamingConventionMatch(
                            namingConvention,
                            commandPropertyName,
                            commandPropertyConflictMatch,
                            MemberMatch<IMember, DefaultMatchKind>.Invalid(),
                            inspectedMembers,
                            requireCanExecuteMatch );
                    }
                    else
                    {
                        return new CommandNamingConventionMatch(
                            namingConvention,
                            commandPropertyName,
                            commandPropertyConflictMatch,
                            MemberMatch<IMember, DefaultMatchKind>.NotFound( matchCanExecuteNamePredicate.Candidates ),
                            inspectedMembers,
                            requireCanExecuteMatch );
                    }
                }
        }
    }

    private static DefaultMatchKind? IsValidCanExecuteMethod( IMethod method )
        => method.ReturnType.SpecialType == SpecialType.Boolean
           && method.Parameters is [] or [{ RefKind: RefKind.None or RefKind.In }]
           && method.TypeParameters.Count == 0
            ? DefaultMatchKind.Default
            : null;

    private static DefaultMatchKind? IsValidCanExecuteProperty( IProperty property )
        => property.Type.SpecialType == SpecialType.Boolean
           && property.GetMethod != null
            ? DefaultMatchKind.Default
            : null;
}