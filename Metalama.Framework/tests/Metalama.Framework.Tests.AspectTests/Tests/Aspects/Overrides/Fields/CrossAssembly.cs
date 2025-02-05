namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Fields.CrossAssembly
{
    // <target>
    [Override]
    [Introduction]
    internal class TargetClass
    {
        public string? ExistingField;

        public string? ExistingField_ReadOnly;

        public string ExistingField_Initializer = "42";

        public static string? ExistingField_Static;
    }
}