internal class TargetClass
{
  [Void]
  [Dynamic]
  public void SyncVoid()
  {
    global::System.Console.WriteLine("dynamic");
    global::System.Console.WriteLine("void");
    Console.WriteLine("This is the original method.");
    return;
  }
  [Void]
  [Dynamic]
  [Task]
  [TaskDynamic]
  public async void AsyncVoid()
  {
    global::System.Console.WriteLine("dynamic");
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Task");
    global::System.Console.WriteLine("Task<dynamic>");
    global::System.Console.WriteLine("void");
    await Task.Yield();
    Console.WriteLine("This is the original method.");
    return;
  }
  [Dynamic]
  public int Int()
  {
    global::System.Console.WriteLine("dynamic");
    Console.WriteLine("This is the original method.");
    return 42;
  }
  [Dynamic]
  [Task]
  [TaskDynamic]
  public Task SyncTask()
  {
    global::System.Console.WriteLine("dynamic");
    return this.SyncTask_Task();
  }
  private global::System.Threading.Tasks.Task SyncTask_TaskDynamic()
  {
    global::System.Console.WriteLine("Task<dynamic>");
    Console.WriteLine("This is the original method.");
    return Task.CompletedTask;
  }
  private async global::System.Threading.Tasks.Task SyncTask_Task()
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Task");
    await this.SyncTask_TaskDynamic();
  }
  [Dynamic]
  [Task]
  [TaskDynamic]
  public async Task AsyncTask()
  {
    global::System.Console.WriteLine("dynamic");
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Task");
    global::System.Console.WriteLine("Task<dynamic>");
    await Task.Yield();
    Console.WriteLine("This is the original method.");
    return;
  }
  [Dynamic]
  [TaskDynamic]
  public Task<int> SyncTaskInt()
  {
    global::System.Console.WriteLine("dynamic");
    global::System.Console.WriteLine("Task<dynamic>");
    Console.WriteLine("This is the original method.");
    return Task.FromResult(42);
  }
  [Dynamic]
  [TaskDynamic]
  public async Task<int> AsyncTaskInt()
  {
    global::System.Console.WriteLine("dynamic");
    global::System.Console.WriteLine("Task<dynamic>");
    await Task.Yield();
    Console.WriteLine("This is the original method.");
    return 42;
  }
  [Dynamic]
  [Task]
  [TaskDynamic]
  public ValueTask SyncValueTask()
  {
    global::System.Console.WriteLine("dynamic");
    return this.SyncValueTask_Task();
  }
  private global::System.Threading.Tasks.ValueTask SyncValueTask_TaskDynamic()
  {
    global::System.Console.WriteLine("Task<dynamic>");
    Console.WriteLine("This is the original method.");
    return new ValueTask();
  }
  private async global::System.Threading.Tasks.ValueTask SyncValueTask_Task()
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Task");
    await this.SyncValueTask_TaskDynamic();
  }
  [Dynamic]
  [Task]
  [TaskDynamic]
  public async ValueTask AsyncValueTask()
  {
    global::System.Console.WriteLine("dynamic");
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Task");
    global::System.Console.WriteLine("Task<dynamic>");
    await Task.Yield();
    Console.WriteLine("This is the original method.");
    return;
  }
  [Dynamic]
  [TaskDynamic]
  public ValueTask<int> SyncValueTaskInt()
  {
    global::System.Console.WriteLine("dynamic");
    global::System.Console.WriteLine("Task<dynamic>");
    Console.WriteLine("This is the original method.");
    return new ValueTask<int>(42);
  }
  [Dynamic]
  [TaskDynamic]
  public async ValueTask<int> AsyncValueTaskInt()
  {
    global::System.Console.WriteLine("dynamic");
    global::System.Console.WriteLine("Task<dynamic>");
    await Task.Yield();
    Console.WriteLine("This is the original method.");
    return 42;
  }
}
