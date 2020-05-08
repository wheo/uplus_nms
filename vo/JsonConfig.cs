using System;
using System.Collections.Generic;
using System.Text;

namespace scte_104_inserter.vo
{
	class JsonConfig
	{
		public String ipAddr { get; set; }
		public Int32 port { get; set; }
		
		public int eventId { get; set; }
		public int uniquePid { get; set; }
		public int preRollTime { get; set; }		
		public int breakDuration { get; set; }

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
