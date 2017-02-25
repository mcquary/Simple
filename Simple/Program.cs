using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Simple
{
    public class Words
    {
        public Guid Id { get; set; }
        public string Word { get; set; }
    }

    public class PartOfSpeech
    {
        public long ID { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
        
        }
    }
}
