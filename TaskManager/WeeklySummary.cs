namespace TaskManager;

using Spectre.Console;

public class WeeklySummary
{
    

    private readonly TaskService _service;

    public WeeklySummary(TaskService service)
    {
        _service = service;
    }

    public bool HasData()
    {
        return _service.GetWeeklyGroups().Any();
    }

    public void DisplaySelection(TaskService service)
    {
        while (true)
        {
            if (!HasData())
            {
                AnsiConsole.MarkupLine("[yellow]No weekly data available.[/]");
                return;
            }

            var prompt = new SelectionPrompt<string>()
                .Title("Select a week:")
                .PageSize(10);

            prompt.AddChoice("Cancel");

            foreach (var group in _service.GetWeeklyGroups())
            {
                var label = $"{group.Key.Start:yyyy-MM-dd} to {group.Key.End:yyyy-MM-dd}";
                prompt.AddChoice(label);
            }

            var selected = AnsiConsole.Prompt(prompt);

            if (selected == "Cancel")
                return;

            var selectedIndex = _service.GetWeeklyGroups().FindIndex(g =>
                $"{g.Key.Start:yyyy-MM-dd} to {g.Key.End:yyyy-MM-dd}" == selected);

            var selectedWindow = _service.GetWeeklyGroups()
                .Skip(selectedIndex)
                .Take(4)
                .ToList();

            DisplayWindow(selectedWindow);

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("\nSelect option:")
                    .AddChoices("Delete Entry", "Return"));

            if (action == "Return")
                return;

            HandleDeleteFlow(service);
        }
    }

    private void DisplayWindow(List<IGrouping<WeekWindow, TaskLog>> window)
    {
        var oldestWeek = window.Last().Key.Start;
        var newestWeek = window.First().Key.End;

        AnsiConsole.Clear();

        AnsiConsole.Write(new Rule(
            $"[bold yellow]TOTAL AGGREGATES ({oldestWeek:yyyy-MM-dd} to {newestWeek:yyyy-MM-dd})[/]"));

        DisplayAggregateSection(window.SelectMany(g => g));

        AnsiConsole.WriteLine();

        AnsiConsole.Write(new Rule("[bold cyan]WEEKLY BREAKDOWN[/]"));

        foreach (var week in window)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine(
                $"[bold]Week {week.Key.Start:yyyy-MM-dd} to {week.Key.End:yyyy-MM-dd}[/]");

            DisplayAggregateSection(week);
        }
    }

    private void DisplayAggregateSection(IEnumerable<TaskLog> logs)
    {
        var categoryTable = new Table();
        categoryTable.AddColumn("Category");
        categoryTable.AddColumn("Time Spent");

        var categoryGroups = logs
            .GroupBy(t => t.Task.Category?.Name ?? "Uncategorized")
            .Select(g => new
            {
                Category = g.Key,
                Total = TimeSpan.FromTicks(g.Sum(t => t.Duration.Ticks))
            })
            .OrderByDescending(x => x.Total);

        foreach (var cat in categoryGroups)
            categoryTable.AddRow(cat.Category, cat.Total.ToString(@"hh\:mm\:ss"));

        AnsiConsole.Write(categoryTable);
        AnsiConsole.WriteLine();

        var taskTable = new Table();
        taskTable.AddColumn("Task");
        taskTable.AddColumn("Time Spent");

        var taskGroups = logs
            .GroupBy(t => t.Task.Name)
            .Select(g => new
            {
                Task = g.Key,
                Total = TimeSpan.FromTicks(g.Sum(t => t.Duration.Ticks))
            })
            .OrderByDescending(x => x.Total);

        foreach (var task in taskGroups)
            taskTable.AddRow(task.Task, task.Total.ToString(@"hh\:mm\:ss"));

        AnsiConsole.Write(taskTable);
    }

    private void HandleDeleteFlow(TaskService service)
    {
        var allLogs = _service.GetWeeklyGroups()
            .SelectMany(g => g)
            .OrderByDescending(l => l.StartTime)
            .ToList();

        if (!allLogs.Any())
        {
            AnsiConsole.MarkupLine("[yellow]No entries to delete.[/]");
            return;
        }

        var days = allLogs
            .Select(l => l.StartTime.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        var dayPrompt = new SelectionPrompt<string>()
            .Title("Select a day:")
            .PageSize(10);

        dayPrompt.AddChoice("Cancel");

        foreach (var day in days)
            dayPrompt.AddChoice(day.ToString("yyyy-MM-dd"));

        var selectedDay = AnsiConsole.Prompt(dayPrompt);

        if (selectedDay == "Cancel")
            return;

        var parsedDay = DateTime.Parse(selectedDay);

        var logsForDay = allLogs
            .Where(l => l.StartTime.Date == parsedDay)
            .OrderByDescending(l => l.StartTime)
            .ToList();

        var entryPrompt = new SelectionPrompt<string>()
            .Title("Select entry to delete:")
            .PageSize(15);

        entryPrompt.AddChoice("Cancel");

        foreach (var log in logsForDay)
        {
            var label =
                $"{log.Id} | {log.StartTime:HH:mm} | {log.Task.Name} | {log.Duration:hh\\:mm\\:ss}";

            entryPrompt.AddChoice(label);
        }

        var selectedEntry = AnsiConsole.Prompt(entryPrompt);

        if (selectedEntry == "Cancel")
            return;

        var id = int.Parse(selectedEntry.Split('|')[0].Trim());

        // 🔴 Confirm Delete
        var confirm = AnsiConsole.Confirm(
            "[red]Are you sure you want to delete this entry?[/]",
            defaultValue: false);

        if (!confirm)
        {
            AnsiConsole.MarkupLine("[yellow]Deletion cancelled.[/]");
            return;
        }

        service.DeleteTaskLog(id);

        AnsiConsole.MarkupLine("[red]Entry deleted successfully.[/]");
    }
}