using log4net;
using System;
using System.Collections.Generic;
using System.Text;

namespace scte_104_inserter.vo
{    
    class Scte104
    {
        public static int _index { get; set; }

        public int index { get; set; }
        public int insertType { get; set; }
        public String eventTime { get; set; }
        public String ipAddress { get; set; }
        public Int32 port { get; set; }
        public String eventType { get; set; }
        public String eventId { get; set; }
        public String uniquePid { get; set; }
        public String prerollTime { get; set; }
        public String breakDuration { get; set; }
        public String status { get; set; }
        public bool isCancel { get; }

        public Scte104()
        {
            index = 0;
        }

        public static List<Scte104> instance;

        private static readonly ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public void WriteLvLog()
        {
            logger.Info(String.Format("{0}:{1},eventType({2}),eventId({3}),uniquePid({4}),prerollTime({5}),breakDuration({6}),status({7})"
                , ipAddress
                , port
                , eventType
                , eventId
                , uniquePid
                , prerollTime
                , breakDuration
                , status));
        }

        public static int GetLastIndex()
        {
            return _index;
        }

        public static int IncreseIndex()
        {
            return _index++;
        }

        public static List<Scte104> GetList()
        {
            if ( instance == null)
            {
                instance = new List<Scte104>();
            }

            return instance;
        }
    }
}
