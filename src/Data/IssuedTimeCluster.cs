using System;

namespace EVEMakeMoney.Data
{
    public class IssuedTimeCluster
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int OrderCount { get; set; }
        public double Percentage { get; set; }
    }
}
