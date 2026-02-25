namespace TaskManager;

using Spectre.Console;



public class DailySummary {
    /*
    Handles the logic for generating and displaying a daily summary of tasks. It takes a date and a list of task logs, 
    filters the logs for the specified date, and provides methods to check if there is data, get total time spent on 
    each task, and display the summary in a formatted table.

    ** The table is sorted by total time spent on each task in descending order.
    */
    private readonly IEnumerable<TaskLog> _logs;

    public DateTime Date { get; }


    public DailySummary(DateTime date, IEnumerable<TaskLog> logs) {
        /*
        The constructor takes a date and a list of task logs. It normalizes the date to ensure it only contains
        the date part (without time) and filters the logs to include only those that match the specified date.
        */
        Date = date.Date;
        _logs = logs
            .Where(l => l.StartTime.Date == Date)
            .ToList();
    }

    public bool HasData() {
        return _logs.Any();
    }

    

    public IEnumerable<(string TaskName, TimeSpan Total)> GetTaskTotals() {
    /*
    Returns a list of tasks with their total time spent for the day, ordered by descending time. It groups
     the logs by task name, sums the durations, and then orders the results.
    */
        return _logs
            .GroupBy(l => l.Task.Name)
            .Select(g => (
                TaskName: g.Key,
                Total: TimeSpan.FromTicks(g.Sum(l => l.Duration.Ticks))
            ))
            .OrderByDescending(x => x.Total);
    }



    public void Display() {
        AnsiConsole.Clear();

        AnsiConsole.Write(new Rule($"[bold yellow]Daily Summary - {Date:yyyy-MM-dd}[/]"));

        if (!HasData())
        {
            AnsiConsole.MarkupLine("[yellow]No tasks recorded today.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Task Name");
        table.AddColumn("Time Spent");
        table.AddColumn("Category");

        var totals = GetTaskTotals().OrderByDescending(t => t.Total).ToList();

        
        foreach (var item in totals)
        {
            table.AddRow(
                item.TaskName,
                item.Total.ToString(@"hh\:mm\:ss"),
                _logs.First(l => l.Task.Name == item.TaskName).Task.Category?.Name ?? "Uncategorized"
            );
        }

        AnsiConsole.Write(table);

        // Bar Chart ////////////////////
        var chart = new BarChart()
            .Width(60)
            .Label("[bold yellow]Time Spent Per Task (Minutes)[/]")
            .CenterLabel();

        foreach (var item in totals)
        {
            chart.AddItem(
                item.TaskName,
                (double)item.Total.TotalMinutes,
                Color.Cyan
            );
        }

        AnsiConsole.Write(chart);
    }

}