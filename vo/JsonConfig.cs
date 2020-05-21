using System;
using System.Collections.Generic;
using System.Text;

namespace scte_104_inserter.vo
{
	class JsonConfig
	{
		public String ipAddr { get; set; }
		public Int16 port { get; set; }
		
		public Int32 eventId { get; set; }
		public Int16 uniquePid { get; set; }
		public Int16 preRollTime { get; set; }		
		public Int16 breakDuration { get; set; }

		public String configFileName = "config.json";

		public static JsonConfig instance;

		public static JsonConfig getInstance()
		{
			if (instance == null)
			{
				instance = new JsonConfig();
			}
			return instance;
		}
	}
}
