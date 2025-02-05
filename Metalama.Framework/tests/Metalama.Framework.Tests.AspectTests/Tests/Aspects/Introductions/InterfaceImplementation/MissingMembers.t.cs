// Final Compilation.Emit failed.
// Error CS0535 on `global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MissingMembers.IInterface`: `'TargetClass' does not implement interface member 'IInterface.Event'`
// Error CS0535 on `global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MissingMembers.IInterface`: `'TargetClass' does not implement interface member 'IInterface.Method()'`
// Error CS0535 on `global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MissingMembers.IInterface`: `'TargetClass' does not implement interface member 'IInterface.Property'`
[Introduction]
public class TargetClass : global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.MissingMembers.IInterface
{
}
