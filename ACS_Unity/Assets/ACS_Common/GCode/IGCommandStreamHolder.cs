using System;

namespace ACS_Common.GCode
{
    public interface IGCommandStreamHolder
    {
        public GCommandStream Stream { get; }
        public Action OnStreamUpdate { get; set; }
    }
}