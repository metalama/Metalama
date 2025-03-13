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
    await this.AsyncVoid_Task();
    return;
  }
  private async global::System.Threading.Tasks.ValueTask AsyncVoid_Source()
  {
    await Task.Yield();
    Console.WriteLine("This is the original method.");
  }
  private async global::System.Threading.Tasks.ValueTask AsyncVoid_Void()
  {
    global::System.Console.WriteLine("void");
    await this.AsyncVoid_Source();
  }
  private async global::System.Threading.Tasks.ValueTask AsyncVoid_TaskDynamic()
  {
    global::System.Console.WriteLine("Task<dynamic>");
    await this.AsyncVoid_Void();
    return;
  }
  private async global::System.Threading.Tasks.ValueTask AsyncVoid_Task()
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Task");
    await this.AsyncVoid_TaskDynamic();
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
    await this.AsyncTask_Task();
    return;
  }
  private async Task AsyncTask_Source()
  {
    await Task.Yield();
    Console.WriteLine("This is the original method.");
  }
  private async global::System.Threading.Tasks.Task AsyncTask_TaskDynamic()
  {
    global::System.Console.WriteLine("Task<dynamic>");
    await this.AsyncTask_Source();
    return;
  }
  private async global::System.Threading.Tasks.Task AsyncTask_Task()
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Task");
    await this.AsyncTask_TaskDynamic();
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
    return (await this.AsyncTaskInt_TaskDynamic());
  }
  private async Task<int> AsyncTaskInt_Source()
  {
    await Task.Yield();
    Console.WriteLine("This is the original method.");
    return 42;
  }
  private async global::System.Threading.Tasks.Task<global::System.Int32> AsyncTaskInt_TaskDynamic()
  {
    global::System.Console.WriteLine("Task<dynamic>");
    return (await this.AsyncTaskInt_Source());
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
    await this.AsyncValueTask_Task();
    return;
  }
  private async ValueTask AsyncValueTask_Source()
  {
    await Task.Yield();
    Console.WriteLine("This is the original method.");
  }
  private async global::System.Threading.Tasks.ValueTask AsyncValueTask_TaskDynamic()
  {
    global::System.Console.WriteLine("Task<dynamic>");
    await this.AsyncValueTask_Source();
    return;
  }
  private async global::System.Threading.Tasks.ValueTask AsyncValueTask_Task()
  {
    await global::System.Threading.Tasks.Task.Yield();
    global::System.Console.WriteLine("Task");
    await this.AsyncValueTask_TaskDynamic();
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
    return (await this.AsyncValueTaskInt_TaskDynamic());
  }
  private async ValueTask<int> AsyncValueTaskInt_Source()
  {
    await Task.Yield();
    Console.WriteLine("This is the original method.");
    return 42;
  }
  private async global::System.Threading.Tasks.ValueTask<global::System.Int32> AsyncValueTaskInt_TaskDynamic()
  {
    global::System.Console.WriteLine("Task<dynamic>");
    return (await this.AsyncValueTaskInt_Source());
  }
}