using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cyberh0me.net.IoTCoreSetSettings.Data
{
    public class Configuration
    {
        public string Interface { get; set; }
        public bool DHCP1 { get; set; }
        public string IPAddress { get; set; }
        public string Subnet { get; set; }
        public string Gateway { get; set; }
        public bool DHCP2 { get; set; }
        public string DNS1 { get; set; }
        public string DNS2 { get; set; }

        public Configuration(string Interface)
        {
            this.Interface = Interface;
        }
    }
}
