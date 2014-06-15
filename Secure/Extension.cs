using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BuskerProxy.Secure
{
    public static class Extension
    {
        const char cr = '\r';
        const char lf = '\n'; 
        
        public static string ReadLine(this BinaryReader br)
        {
            char ch='\0';
            string line="";
            while (br.BaseStream.CanRead)
            {
                ch = br.ReadChar();
                if (ch == cr)
                {
                    br.ReadChar();
                    break;
                }
                line += ch;
            }
            return line;
        }

        public static void WriteLine(this BinaryWriter bw, string line="")
        {
            foreach(char ch in line.AsEnumerable())
                bw.Write(ch);
            bw.Write(cr);
            bw.Write(lf);
        }


        public static string ReadLine(this Stream stream)
        {
            var reader = new BinaryReader(stream, Encoding.UTF8, true);
            return reader.ReadLine();
        }

        public static void WriteLine(this Stream stream, string line)
        {
            var writer = new BinaryWriter(stream, Encoding.UTF8, true);
            writer.WriteLine(line);
        }
    }
}
