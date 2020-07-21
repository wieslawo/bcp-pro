using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using Microsoft.VisualBasic.FileIO;
using Mono.Options;

namespace bcp_pro 
{
    /// <summary>
    /// example usage:
    /// bcp_pro dbo.schedule -i D:\temp\MongoExport\EmailSchedulerSvc.ScheduleModel.csv -S . -D Test -T
    /// bcp_pro dbo.schedule -o D:\temp\MongoExport\EmailSchedulerSvc.ScheduleModel.csv -S . -D Test -T
    /// bcp_pro op.schedule -i D:\temp\MongoExport\EmailSchedulerSvc.ScheduleModel.csv -S dw3.copik6lhxrq7.us-east-1.rds.amazonaws.com -D dw -U admin -P xxxx
    /// </summary>
    class bcp_pro
    {
        static string _csvFilePathImport;
        static string _csvFilePathExport;
        static string _serverName;
        static string _database;
        static string _tableName;
        static bool _trustedConnection;
        static string _userName;
        static string _password;

        static void Main(string[] args)
        {
            string genericMessage = "try `bcp_pro  -h' for more information";
            bool showHelp = false;
            List<string> extra;

            var options = new OptionSet {
                "bcp by Wieslaw Olborski, 2020.",
                "Usage: bcp_pro  table + [OPTIONS]",
                "Imports data to MSSQL table from csv file. Support double quotation mark, which is not supported by Microsoft bcp :(.",
                "Export csv file with column names in first row, standard bcp doesn't have such option :(.",
                "Options:",
                { "i|in=", "inputfile", v => _csvFilePathImport = v },
                { "o|out=", "outfile", v => _csvFilePathImport = v },
                { "b|bcp=", "bcp.exe location used for export", v => _csvFilePathImport = v },
                { "D|database=", "database", v => _database = v },
                { "S|server=", "server name", v => _serverName = v },
                { "T", "trusted connection", v => _trustedConnection = v != null },
                { "U|username=", "username", v => _userName = v },
                { "P|password=", "password", v => _password = v },
                { "h",  "show this message and exit", v => showHelp = v != null },
            };
            
            try {
                extra = options.Parse (args);
            }
            catch (OptionException e) {
                Console.Write ("bcp_pro : ");
                Console.WriteLine (e.Message);
                Console.WriteLine (genericMessage);
                return;
            }

            if (showHelp) {
                options.WriteOptionDescriptions (Console.Out);
                return;
            }
            
            if (extra.Count== 1 && 
                !string.IsNullOrWhiteSpace(_csvFilePathImport) && 
                !string.IsNullOrWhiteSpace(_serverName) && 
                (_trustedConnection || !string.IsNullOrWhiteSpace(_userName) && 
                 !string.IsNullOrWhiteSpace(_password))
                )
            {
                _tableName = extra[0];
                Console.WriteLine("bcp_pro - started read csv file.");
                var csvData = GetDataTabletFromCsv(_csvFilePathImport);
                Console.WriteLine($"bcp_pro - csv file rows for import: {csvData.Rows.Count}.");
                if (csvData != null)
                {
                    InsertDataToSqlServer(csvData);
                }
                Console.WriteLine("bcp_pro - done.");
            }
            else {
                Console.WriteLine(genericMessage);
            }
        }

        private static DataTable GetDataTabletFromCsv(string csvFilePath)
        {
            DataTable csvData = new DataTable();
            try
            {
                using(TextFieldParser csvReader = new TextFieldParser(csvFilePath))
                {
                    csvReader.SetDelimiters(",");
                    csvReader.HasFieldsEnclosedInQuotes = true;
                    string[] colFields = csvReader.ReadFields();
                    foreach (string column in colFields)
                    {
                        DataColumn datecolumn = new DataColumn(column);
                        datecolumn.AllowDBNull = true;
                        csvData.Columns.Add(datecolumn);
                    }
                    while (!csvReader.EndOfData)
                    {
                        string[] fieldData = csvReader.ReadFields();
                        
                        // Making empty value as null
                        for (int i = 0; i < fieldData.Length; i++)
                        {
                            if (fieldData[i] == "")
                            {
                                fieldData[i] = null;
                            }
                        }

                        csvData.Rows.Add(fieldData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return null;
            }
            return csvData;
        }

        static void InsertDataToSqlServer(DataTable csvFileData)
        {
            var credentials = "Integrated Security=SSPI;";
            if (!_trustedConnection)
            {
                credentials = $"User ID={_userName};Password={_password};";
            }

            using (SqlConnection dbConnection =
                new SqlConnection($"Data Source={_serverName};Initial Catalog={_database};{credentials}"))
            {
                dbConnection.Open();
                using (SqlBulkCopy s = new SqlBulkCopy(dbConnection, SqlBulkCopyOptions.KeepNulls, null))
                {
                    s.BatchSize = 10000;
                    s.DestinationTableName = _tableName;
                    s.NotifyAfter = 10000;
                    s.BulkCopyTimeout = 0;
                    s.SqlRowsCopied += OnSqlRowsCopied;
                    s.WriteToServer(csvFileData);
                }
            }
        }

        private static void OnSqlRowsCopied(
            object sender, SqlRowsCopiedEventArgs e)
        {
            Console.WriteLine("Copied {0:##,###} rows to SQL Server.", e.RowsCopied);
        }

        static void Export()
        {
            https://stackoverflow.com/questions/3921334/how-to-bulk-copy-sql-export-tables-to-a-csv-or-tsv-files-in-net
            System.Diagnostics.Process p = new System.Diagnostics.Process();             
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.FileName = "BCP.exe";
            p.StartInfo.Arguments = "\"SELECT * FROM DATABASENAME.dbo.TABLENAME\" queryout \"FILENAME.txt\" -S \"SEVERNAMEHERE\" -U USERNAME -P PASSWORD -c -k";
            p.Start(); 
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
        }
         
    }
}
