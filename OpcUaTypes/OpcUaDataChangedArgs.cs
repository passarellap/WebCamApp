using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OpcUaTypes
{
    public class OpcUaDataChangedArgs : EventArgs
    {
        public OpcUaDataChangedArgs(string name, string addr, JObject value)
        {
            varName = name;
            varAddr = addr;
            varValue = value;
        }
        private string varName, varAddr;
        private JObject varValue;
        public string VarName
        {
            get { return varName; }
        }
        public string VarAddr
        {
            get { return varAddr; }
        }
        public JObject VarValue
        {
            get { return varValue; }
        }
    }
}
