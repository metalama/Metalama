using Dependency;

namespace Tests;

[OverrideWithPrivateTemplates]
class OverrideWithPrivateTemplatesTest
{
    private int _field;
    public void SomeMethod() { }
    public int SomeProperty { get; set; }
}