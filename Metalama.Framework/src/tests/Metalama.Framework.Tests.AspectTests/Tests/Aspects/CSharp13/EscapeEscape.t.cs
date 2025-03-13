class Target
{
  [TheAspect]
  void M()
  {
    global::System.Console.WriteLine("\e[1mThis is bold text from template.\e[0m");
    Console.WriteLine("\e[3mThis is italic text from target.\e[0m");
    return;
  }
}