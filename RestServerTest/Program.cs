using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Library;
using Library.db;

namespace RestServerTest
{
    public static class Program
    {
        

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DataBase.DataBaseType = DataBaseType.SQLite;
            DataBase.DataBasePath = lib.AppDirectory;
            //DataBase.DataBasePath = @"C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\Common7\IDE\Extensions\TestPlatform";
            
            DataBase.DataBaseName = "taskmgmt.sqlite";
            DataBase.SynchronizeDataBase = false;

           

            string yaml = lib.ReadEmbeddedTextFile("taskmgmt.yaml");

            RESTServer rs = new RESTServer(yaml);

            rs.StartListening("resttest",8090);
            
            if (args.Count() == 0)
            {
                MessageBox.Show("started. click ok to close");
            }
        }
    }
}
