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
        public Int16 port { get; set; }
        public String eventType { get; set; }
        public int eventId { get; set; }
        public Int16 uniquePid { get; set; }
        public Int16 prerollTime { get; set; }
        public Int16 breakDuration { get; set; }
        public String status { get; set; }
        public bool isCancel { get; }

        public Scte104()
        {
            index = 0;
        }
        public Scte104 ShallowCopy()
        {
            return (Scte104)this.MemberwiseClone();
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

        public int SetIncrease()
        {
            index = _index++;
            return index;
        }

        public static int GetLastIndex()
        {
            return _index;
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
