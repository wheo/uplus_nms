using System;
using System.Collections.Generic;
using System.Text;

namespace uplus_nms.vo
{    
    class Log
    {        
        public DateTime eventTime { get; set; }
        public String ipAddress { get; set; }
        public Int32 port { get; set; }
        public String eventType { get; set; }
        public String eventId { get; set; }
        public String uniquePid { get; set; }
        public String prerollTime { get; set; }
        public String breakDuration { get; set; }
        public String status { get; set; }

        public List<Log> instance;

        public List<Log> GetList()
        {
            if ( instance == null)
            {
                instance = new List<Log>();
            }

            return instance;
        }
    }
}
