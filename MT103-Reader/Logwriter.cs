using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MT103_Reader
{
    public static class Logwriter
    {

        public static void WriteErrorLog(Exception ex)
        {
            StreamWriter sw = null;
            var dte = DateTime.Now.ToString();

            try {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogFile.txt", true);
                sw.WriteLine( dte + ":" + ex.Source.ToString().Trim() + ";" + ex.Message.ToString().Trim());
                Console.WriteLine( dte + ":" + ex.Source.ToString().Trim() + ";" + ex.Message.ToString().Trim() );
                sw.Flush();
                sw.Close();

            } catch {


            }
        }

        public static void WriteErrorLog(string message) {
            StreamWriter sw = null;
            var dte = DateTime.Now.ToString();

            try {
                sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\LogFile.txt", true);
                sw.WriteLine(dte + ": " + message);
                Console.WriteLine( dte + ": " + message );
                sw.Flush();
                sw.Close();
            } catch {


            }
        }
    }
}
