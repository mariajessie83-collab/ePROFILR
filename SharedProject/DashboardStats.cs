using System;
using System.Collections.Generic;

namespace SharedProject
{
    public class DashboardStatistics
    {
        public List<StatusStat> StatusCounts { get; set; } = new();
        public List<BehaviorStat> TopBehaviors { get; set; } = new();
        public List<WeeklyStat> WeeklyStats { get; set; } = new();
        public List<YearlyStat> YearlyStats { get; set; } = new();
    }

    public class StatusStat
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class BehaviorStat
    {
        public string Behavior { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class WeeklyStat
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class YearlyStat
    {
        public string Year { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
