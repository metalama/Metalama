using Metalama.Framework.Aspects;
using System;
namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.CSharp11.FileLocalType_Override;
// A declaration of a file-local type is identified by a SerializableDeclarationId that carries the metadata name of
// that type, so that two file-local types of the same name declared in two files are told apart. See issue #662.
// The compile-time pipeline never needed that identifier for an override, so this test guards a case that already
// worked. The identifier is what the design-time pipeline requires, and its absence cost the whole project its
// Metalama features in the editor. See issue #2051.
#pragma warning disable CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
public class LogAttribute : OverrideMethodAspect
{
  public override dynamic? OverrideMethod() => throw new System.NotSupportedException("Compile-time-only code cannot be called at run-time.");
}
#pragma warning restore CS0067, CS8618, CS0162, CS0169, CS0414, CA1822, CA1823, IDE0051, IDE0052
file class FileLocalTarget
{
  [Log]
  public int Add(int a, int b)
  {
    global::System.Console.WriteLine("Add started.");
    return a + b;
  }
  [Log]
  public T Echo<T>(T value)
  {
    global::System.Console.WriteLine("Echo started.");
    return value;
  }
}
public class Caller
{
  public int Call() => new FileLocalTarget().Add(1, 2);
}