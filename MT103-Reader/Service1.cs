using Sybase.Data.AseClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MT103_Reader {
    public partial class Service1 : ServiceBase {

        Timer timer = new Timer();

        static private string sourceFolder;
        static private string archiveFolder;
        static private string splitFolder;
        static private string connString;
        
        static private FileInfo[] fileList = new FileInfo[0];

        public Service1() {
           // ReadFile(@"C:\Users\herbert.onuoha\Documents\varification.txt");
        }

        public void onDebug()
        {
            OnStart(null);
        }

        
        protected override void OnStart(string[] args) {

            timer.Interval = 3000; // 3 second 
            Console.WriteLine($"Timer started =>");
            timer.Elapsed += new ElapsedEventHandler(this.timer1_Tick);
            Console.WriteLine($"Timer ticked");
            timer.Start();
            Console.WriteLine($"Timer full start");
            Logwriter.WriteTolog("Service Started successfully");

        }

        /// <summary>
        /// The timer processe prt files for MT103/MT202, ETC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {

            //init
            sourceFolder    = ConfigurationManager.AppSettings["sourceFolder"].ToString();
            archiveFolder   = ConfigurationManager.AppSettings["archiveFolder"].ToString();
            splitFolder     = ConfigurationManager.AppSettings["splitFolder"].ToString();
            connString      = ConfigurationManager.ConnectionStrings["sybaseconnection"].ToString();
            
        ///// <summary>
        ///// This does the following
        ///// 1. Pulls all files from the folder - @sourceFolder configured in app.config (into a list
        ///// 2. Checks if the file has been previously porcessed
        /////     a. If YES, moves the file to a different folder - @archiveFolder
        /////     b. If NO, processes the file as per recommendations and then performs (a) above
        /////  3. Ends the process
        /////  4. Logging to file is enabled at every point in the process
        ///// </summary>
           
         processTransactions();

        }
        internal void processTransactions() { 
            //get the list of files
            try {

                fileList = new DirectoryInfo(sourceFolder).GetFiles("*.*");

                if ( fileList.Count() > 0 ) {
                    //process the retrieved files
                    processFiles( fileList: fileList );


                }

            } catch ( Exception ex ) {
                Logwriter.WriteTolog("error getting files in source folder" + ex.Message);
            }
        }

        internal void processFiles ( FileInfo[] fileList ) {
            try {
                string oldestFile       = "";                  
                DateTime lastModified   = DateTime.MaxValue;

                foreach ( FileInfo file in fileList ) {
                    if ( file.LastWriteTime < lastModified ) {
                        lastModified    = file.LastWriteTime;
                        oldestFile      = file.Name.Replace(".prt", "");

                        //check if the file has been process in the db
                        if ( !checkIfFileExistsInDB( oldestFile, lastModified ) ){
                            //process the file
                            readAndSplitFile( sourceFolder, oldestFile , lastModified);

                        }

                        //move to archive folder -@archiveFolder
                        File.Move(  sourceFolder + oldestFile + ".prt" ,  archiveFolder + oldestFile + ".prt" );        
                    }
                }

            } catch (Exception ex ) {
                Logwriter.WriteTolog("unable to process file from source folder" + ex.Message);
            }
        }

        internal bool checkIfFileExistsInDB( string oldestFile, DateTime lastModified ) {
            
            try {

                string query =  " SELECT * " +
                                " FROM zib_mt103acknew_files " +
                                " WHERE (FILE_NAME = '{0}') " +
                                " AND year (import_dt) = {1} " +  
                                " AND month (import_dt) = {2} " +  
                                " AND day (import_dt)= {3} ";
                query = string.Format(query, oldestFile, lastModified.Year, lastModified.Month, lastModified.Day );

                using ( AseConnection connection = new AseConnection(connString)) {
                    connection.Open();

                    using (AseCommand command = new AseCommand(query, connection)) {

                        using (AseDataReader reader = command.ExecuteReader()) {

                            if (reader.HasRows) {
                                return true;
                            }
                        }
                    }
                    connection.Close();
                }

            } catch (Exception e) {
                Logwriter.WriteTolog("unable to check if file exists in database" + e.Message);
            }

            return false;

        }
       
        internal static void readAndSplitFile(string filePath, string OldestParentFile, DateTime import_dt) 
        {

            int add = 1;
            try 
             {
                

                string Readall = File.ReadAllText((string.Concat(filePath, OldestParentFile + ".prt")));
            
                   string[] stringSeparators = new string[] { "---------------------  Instance Type and Transmission --------------" };  

                    List<string> Tokens = Readall.Split(stringSeparators, StringSplitOptions.None ).ToList(); 
                
                    //Remove first token because it is not needed
                      Tokens.RemoveAt(0);
                     for(int a=0; a<Tokens.Count(); a++) {

                        Tokens[a]=  "       ---------------------  Instance Type and Transmission ---------------" + System.Environment.NewLine + Tokens[a];

                        string NewSplitFile = ((OldestParentFile) +"_" + add++);
                         string lastSplitFile = NewSplitFile.ToString();

                           //Write split file to MT103ACKSPLIT folder

                         File.WriteAllText(string.Concat(splitFolder, lastSplitFile.ToString(), ".TXT"), Tokens[a]);


                      //get data from the split file
                      
                   
                        try
                            {
                            string linefield20   = string.Empty;
                            string linefield50   = string.Empty;
                            bool   ProcessStart  = false;
                            string field20       = string.Empty;
                            string field50       = string.Empty;
                            string msgType       = string.Empty;
                            string send_attach   = "Y";
                            int    FileAmount    = 1;



                               using (StreamReader file = new StreamReader(string.Concat(splitFolder,lastSplitFile, ".txt")))
                                    {
                                        string line;
                                        while ( (line = file.ReadLine()) != null)
                                        {

                                                    if (line.Contains(": FIN"))
                                                    {   string FinType = line.Trim(); line = file.ReadLine(); 
                                                        string [] firstSplit = FinType.Split(':');
                                                        string [] secondSplit = firstSplit[1].Split(' '); 
                                                        msgType = secondSplit[2];      
                                                    }   


                                            if(line.Contains("---------------------------- Interventions -------------------------")) ProcessStart = true; 
                                    
                                            while(ProcessStart) 
                                            {
                                                    if (line.Contains(":20:"))
                                                    {  linefield20 = line.Trim(); line = file.ReadLine(); }

                                                    if (line.Contains(":50"))
                                                    {  linefield50 = line.Trim(); line = file.ReadLine(); ProcessStart = false; break; }

                                                     line = file.ReadLine();
                                            }
                                             ProcessStart = false;
                                          }
                                        
                                        file.Close();
                                    }
                                            field20  = linefield20.Substring(4);
                                            field50  = linefield50.Substring(6);
                         
                                //now insert ref details into  ref table

                                string RefFile =  ConfigurationManager.ConnectionStrings["sybaseconnection"].ToString();

                                 using (AseConnection connection = new AseConnection(RefFile))
                                    {
                                        using(AseCommand cmd = connection.CreateCommand())
                                        {
                                            cmd.CommandText = "zsp_swift_mt103acknew_ref";
                                            cmd.CommandType = CommandType.StoredProcedure;
                                            cmd.Parameters.AddWithValue("@msg_type", msgType);
                                            cmd.Parameters.AddWithValue("@field20", field20);
                                            cmd.Parameters.AddWithValue("@field50", field50);
                                            cmd.Parameters.AddWithValue("@send_attach", send_attach);
                                            cmd.Parameters.AddWithValue("@no_of_file", FileAmount); 
                                            cmd.Parameters.AddWithValue("@file_name", lastSplitFile);
                                            cmd.Parameters.AddWithValue("@import_dt", DateTime.Now);
                                            cmd.Parameters.AddWithValue("@partner_no", string.Empty);
                                            cmd.Parameters.Add("@rsErrorText",AseDbType.VarChar).Direction = ParameterDirection.Output;
                                

                                            if (connection.State != ConnectionState.Open){connection.Open();}
                                            cmd.ExecuteNonQuery();
                                            connection.Close();
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                   //Logwriter.WriteTolog(ex.InnerException);
                                   Debug.WriteLine(ex);
                                 }
            
        
                                // insert record into split table

                            string SplitFile =  ConfigurationManager.ConnectionStrings["sybaseconnection"].ToString();

                            try {

                                //removing the prt extension, so we can insert just the filename without the extension in database
                                    if (OldestParentFile.Contains(".prt"))
                                        {
                                          var changeparent = OldestParentFile.ToString();
                                          var filename = changeparent.Replace(".prt", "");
                                           OldestParentFile = filename;
                                        }



                                using (AseConnection connection = new AseConnection(SplitFile)) {
                                    connection.Open();
                                    AseCommand insertCmd = new AseCommand(
                                       "INSERT INTO  zib_mt103acknew_split " +
                                       " ( file_name, split_name, create_dt, import_dt) " +
                                       " VALUES( @file_name, @split_name,getdate(), @import_dt )", 
                                       connection);
                                    insertCmd.Parameters.Add("@file_name", AseDbType.VarChar).Value  = OldestParentFile;
                                    insertCmd.Parameters.Add("@split_name", AseDbType.VarChar).Value = NewSplitFile;
                                    //insertCmd.Parameters.Add("@create_dt", AseDbType.Date).Value   = DateTime.Now;
                                    insertCmd.Parameters.Add("@import_dt", AseDbType.Date).Value     = DateTime.Now;

                                     if (connection.State != ConnectionState.Open)
                                        connection.Open();

                                    int i = insertCmd.ExecuteNonQuery();

                                    connection.Close();
                                }
                            } catch (Exception ex) {
                                Logwriter.WriteTolog("Error in inserting last split file : " + ex.InnerException);
                                Logwriter.WriteTolog("Error in inserting last split file : " + ex.Message);

                                Console.WriteLine($"Exception 1 => " + ex.Message);
                            }

                        }

                           // insert record into .prt file table
                             string PRTfile =  ConfigurationManager.ConnectionStrings["sybaseconnection"].ToString();
                  
                             try {

                                    //removing the prt extension, so we can insert just the filename without the extension in database
                                        if (OldestParentFile.Contains(".prt"))
                                            {
                                              var changeparent     = OldestParentFile.ToString();
                                              var filename         = changeparent.Replace(".prt", "");
                                                  OldestParentFile = filename;
                                            }



                                    using (AseConnection connection = new AseConnection(PRTfile)) {
                                        connection.Open();
                                        AseCommand insertCmd = new AseCommand(
                                           "INSERT INTO  zib_mt103acknew_files " +
                                           " ( file_name, create_dt, import_dt,no_of_file) " +
                                           " VALUES( @file_name,getdate(), @import_dt, @no_of_file )", 
                                           connection);
                                        insertCmd.Parameters.Add("@file_name", AseDbType.VarChar).Value    = OldestParentFile;
                                       // insertCmd.Parameters.Add("@create_dt", AseDbType.Date).Value     = DateTime.Now;
                                        insertCmd.Parameters.Add("@import_dt", AseDbType.Date).Value       = import_dt;
                                        insertCmd.Parameters.Add("@no_of_file", AseDbType.Integer).Value   = Tokens.Count;

                                         if (connection.State != ConnectionState.Open)
                                            connection.Open();

                                        int i = insertCmd.ExecuteNonQuery();

                                        connection.Close();
                                    }
                                } catch (Exception ex) {
                                    Logwriter.WriteTolog("Error in inserting last split file : " + ex.Message);
                                    Debug.WriteLine(ex);        
                                    Console.WriteLine($"Exception 1 => " + ex.Message);
                                }

                           //now move last read parent file to a new folder
                              try
                                {
                                    //File.Copy(source,destination, true);

                                    //   File.Delete(source);

                                    File.Move(filePath + OldestParentFile + ".prt" , archiveFolder + OldestParentFile + ".prt" );
              
                                }
                                    catch (FileNotFoundException e)
                                    {
                                       Debug.WriteLine(e);
                                      //Process.Start("cmd.exe","/c taskkill /MT103-Reader.exe"); 
                                    }
                
                                    }  catch ( Exception ex ) 
                                 {
                                   Debug.WriteLine(ex.Message);
                                 }
        }

        protected override void OnStop() {

        }

        public void TestInConsole(string[] args)
        {
            Console.WriteLine($"Service starting...");
            this.OnStart(args);
            Console.WriteLine($"Service started. Press any key to stop.");
            Console.ReadKey();
            Console.WriteLine($"Service stopping...");
            this.OnStop();
            Console.WriteLine($"Service stopped. Closing in 5 seconds.");
            //System.Threading.Thread.Sleep(5000);
        }

    }
}
