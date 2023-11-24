using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaTypes
{
	public class OpcUaConnectionParams
	{
		public string ServerAddress { get; set; }
		public ushort ServerNamespace { get; set; }
		public bool WaitConnected { get; set; } = true;
		public bool Enabled { get; set; } = true;
        public string ConfigFilesPath { get; set; }
    }
}
