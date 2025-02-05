[Introduction]
[Test]
public class TargetClass : IBase0Interface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Hierarchy.IInterface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Hierarchy.IBase2Interface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Hierarchy.IBase1Interface, global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Introductions.InterfaceImplementation.Hierarchy.IBase3Interface<global::System.Int32>
{
  public void Foo()
  {
    Console.WriteLine("Original interface member");
  }
  public void Bar()
  {
    global::System.Console.WriteLine("Introduced interface member");
  }
  public void Evaluator()
  {
    global::System.Console.WriteLine("Target type implements IBase0Interface: True");
    global::System.Console.WriteLine("Target type implements IBase1Interface: True");
    global::System.Console.WriteLine("Target type implements IBase2Interface: True");
    global::System.Console.WriteLine("Target type implements IBase3Interface<int>: True");
    global::System.Console.WriteLine("Target type implements IInterface: True");
    global::System.Console.WriteLine("ImplementedInterfaces contains IBase0Interface.");
    global::System.Console.WriteLine("ImplementedInterfaces contains IInterface.");
    global::System.Console.WriteLine("ImplementedInterfaces contains IBase2Interface.");
    global::System.Console.WriteLine("ImplementedInterfaces contains IBase1Interface.");
    global::System.Console.WriteLine("ImplementedInterfaces contains IBase3Interface<int>.");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IBase0Interface.");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IInterface.");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IBase2Interface.");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IBase1Interface.");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IBase3Interface<int>.");
  }
  public void Goo()
  {
    global::System.Console.WriteLine("Introduced interface member");
  }
  public void Quz()
  {
    global::System.Console.WriteLine("Introduced interface member");
  }
  public void Zoo()
  {
    global::System.Console.WriteLine("Introduced interface member");
  }
}
[Test]
public class DerivedClass : TargetClass
{
  public new void Evaluator()
  {
    global::System.Console.WriteLine("Target type implements IBase0Interface: True");
    global::System.Console.WriteLine("Target type implements IBase1Interface: True");
    global::System.Console.WriteLine("Target type implements IBase2Interface: True");
    global::System.Console.WriteLine("Target type implements IBase3Interface<int>: True");
    global::System.Console.WriteLine("Target type implements IInterface: True");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IBase0Interface.");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IInterface.");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IBase2Interface.");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IBase1Interface.");
    global::System.Console.WriteLine("AllImplementedInterfaces constains IBase3Interface<int>.");
  }
}