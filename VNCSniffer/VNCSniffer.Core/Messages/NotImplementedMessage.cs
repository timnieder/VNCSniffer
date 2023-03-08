using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VNCSniffer.Core.Messages
{
    public class NotImplementedMessage : IVNCMessage
    {
        public string Name;
        public NotImplementedMessage(string name) { Name = name; }
        public Messages.ProcessStatus Handle(Messages.MessageEvent e)
        {
            throw new NotImplementedException($"Message: {Name}");
        }
    }
}
