using Xunit;
using TaskManager;
using System.Linq;
using System.Threading;

namespace TaskManager.Tests;



public class TaskServiceTests
{
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        var fakeStorage = new FakeStorageService();
        _service = new TaskService(fakeStorage);
    }

    [Fact]
    public void StartTask_SetsCurrentTask()
    {
        var result = _service.StartTask("Test Task", 1);

        Assert.True(result);
        Assert.NotNull(_service.CurrentTask);
        Assert.Equal(TaskState.Running, _service.CurrentTask!.State);
    }

    [Fact]
    public void StartTask_Fails_IfAlreadyRunning()
    {
        _service.StartTask("Task1", 1);
        var result = _service.StartTask("Task2", 1);

        Assert.False(result);
    }

    [Fact]
    public void PauseTask_ChangesStateToPaused()
    {
        _service.StartTask("Task", 1);

        Thread.Sleep(10);

        _service.PauseTask();

        Assert.Equal(TaskState.Paused, _service.CurrentTask!.State);
        Assert.Null(_service.CurrentTask.LastResumedAt);
        Assert.True(_service.CurrentTask.TotalActiveTime.TotalMilliseconds > 0);
    }



    [Fact]
    public void ResumeTask_ChangesStateBackToRunning()
    {
        _service.StartTask("Task", 1);

        _service.PauseTask();
        _service.ResumeTask();

        Assert.Equal(TaskState.Running, _service.CurrentTask!.State);
        Assert.NotNull(_service.CurrentTask.LastResumedAt);
    }



    [Fact]
    public void CompleteTask_AddsToLogs_AndClearsCurrentTask()
    {
        _service.StartTask("Task", 1);

        Thread.Sleep(10);

        _service.CompleteTask();

        Assert.Null(_service.CurrentTask);

        var todayTasks = _service.GetTodayTasks();
        Assert.Single(todayTasks);
        Assert.Equal(TaskState.Completed, todayTasks.First().State);
    }


    [Fact]
    public void DiscardTask_ClearsCurrentTask()
    {
        _service.StartTask("Task", 1);

        _service.DiscardTask();

        Assert.Null(_service.CurrentTask);
    }


    [Fact]
    public void DeleteTaskLog_RemovesLog()
    {
        _service.StartTask("Task", 1);
        _service.CompleteTask();

        var log = _service.GetTodayTasks().First();

        _service.DeleteTaskLog(log.Id);

        var logs = _service.GetTodayTasks();
        Assert.Empty(logs);
    }

    [Fact]
    public void AddCategory_AddsNewCategory()
    {
        var category = _service.AddCategory("NewCat");

        Assert.Contains(_service.Categories, c => c.Name == "NewCat");
    }

    [Fact]
    public void GetWeeklyGroups_ReturnsGrouping()
    {
        _service.StartTask("Task", 1);
        _service.CompleteTask();

        var groups = _service.GetWeeklyGroups();

        Assert.Single(groups);
        Assert.Single(groups.First());
    }
}