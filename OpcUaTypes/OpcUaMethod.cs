using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaTypes
{
    public class OpcUaMethod
    {      
        public String objNodeId = "";
        public String methodNodeId = "";

        public OpcUaMethod(String objNodeId, String methodNodeId)
        {
            this.objNodeId = objNodeId;
            this.methodNodeId = methodNodeId;
        }
    }
}
