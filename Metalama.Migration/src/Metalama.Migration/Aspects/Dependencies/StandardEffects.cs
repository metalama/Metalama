// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Aspects.Dependencies
{
    /// <summary>
    /// Aspect effects are not supported in Metalama.
    /// </summary>
    public static class StandardEffects
    {
        public const string MemberImport = "MemberImport";

        public const string CustomAttributeIntroduction = "CustomAttributeIntroduction";

        public const string InterfaceIntroduction = "InterfaceIntroduction";

        public const string MemberIntroduction = "MemberIntroduction";

        public const string Custom = "Custom";

        public const string ChangeControlFlow = "ChangeControlFlow";

        public static string GetMemberIntroductionEffect( string memberName )
        {
            return MemberIntroduction + ":" + memberName;
        }

        public static string GetMemberImportEffect( string memberName )
        {
            return MemberImport + ":" + memberName;
        }

        public static string GetInterfaceIntroductionEffect( string typeName )
        {
            return InterfaceIntroduction + ":" + typeName;
        }
    }
}