using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PomodoroTimer.Models
{
    internal class TaskItem
    {
        public string Text { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
    }
}
