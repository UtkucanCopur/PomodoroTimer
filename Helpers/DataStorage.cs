using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PomodoroTimer.Models;

namespace PomodoroTimer.Helpers
{
    internal class DataStorage
    {
        private static readonly string tasksFile = Path.Combine(FileSystem.AppDataDirectory, "tasks.json");
        private static readonly string statsFile = Path.Combine(FileSystem.AppDataDirectory, "stats.json");

        public static async Task SaveTasksAsync(List<TaskItem> tasks)
        {
            var json = JsonSerializer.Serialize(tasks);
            await File.WriteAllTextAsync(tasksFile, json);
        }

        public static async Task<List<TaskItem>> LoadTasksAsync()
        {
            if (!File.Exists(tasksFile))
                return new List<TaskItem>();

            var json = await File.ReadAllTextAsync(tasksFile);
            return JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
        }

        public static async Task SavePomodoroCountAsync(int count)
        {
            await File.WriteAllTextAsync(statsFile, count.ToString());
        }

        public static async Task<int> LoadPomodoroCountAsync()
        {
            if (!File.Exists(statsFile))
                return 0;

            var content = await File.ReadAllTextAsync(statsFile);
            return int.TryParse(content, out int count) ? count : 0;
        }
    }

}
