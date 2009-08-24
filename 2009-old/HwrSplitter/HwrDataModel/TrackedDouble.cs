using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataIO
{
    public enum TrackStatus
    {
        Uninitialized=0, Initialized, Calculated, Manual
    }

    struct TrackedDouble
    {
        public double val;
        public TrackStatus status;
        public TrackedDouble(double init) { val = init; status = TrackStatus.Initialized; }
    }
}
