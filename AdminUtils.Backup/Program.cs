using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Ionic.Zip;
using Ionic.Zlib;
using MySql.Data.MySqlClient;
using Renci.SshNet;

namespace AdminUtils.Backup
{
    class Program
    {
        private static string _fileName = String.Format("{0}/Dump{1}", ConfigurationManager.AppSettings["Path"],
            DateTime.Now.ToString("yyyyMMddhhmmss"));

        static string _fileNameSql = String.Format("{0}.sql", _fileName);
        static string _fileNameData = String.Format("{0}_data.sql", _fileName);
        static string _fileNameZip = String.Format("{0}.zip", _fileName);

        static void Main(string[] args)
        {

            if (!MysqlCreateDump()) return;
            if (!CreateZip()) return;

            var remote = ConfigurationManager.AppSettings["RemoteServer"];
            if (remote != null)
            {
                Console.WriteLine("Try upload to remote server");
                var url = new Uri(remote);
                switch (url.Scheme)
                {
                    case "ftp":
                        FtpUpload(url);
                        break;
                    case "ssh":
                        SshUpload(url);
                        break;
                }
            }
        }

     
        private static bool CreateZip()
        {
            Console.WriteLine("Zipping dump...");
            using (ZipFile zip = new ZipFile())
            {
                zip.CompressionLevel = CompressionLevel.BestCompression;
                try
                {
                    zip.AddFile(_fileNameSql, "");
                    zip.AddFile(_fileNameData, "");
                    zip.Save(_fileNameZip);
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("Can't create zip archive");
                    Console.WriteLine("Details :{0}", e.Message);
                    return false;
                }
            }
            File.Delete(_fileNameSql);
            return true;
        }

        private static bool MysqlCreateDump()
        {
            Console.WriteLine("Creating  dump...");
            using (
                MySqlConnection conn =
                    new MySqlConnection(ConfigurationManager.ConnectionStrings["DefaultContext"].ConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    using (MySqlBackup mb = new MySqlBackup(cmd))
                    {
                        cmd.Connection = conn;
                        try
                        {
                            conn.Open();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Can't connect to MySQL server");
                            Console.WriteLine("Details :{0}", e.Message);
                            return false;
                        }

                        mb.ExportInfo.AddCreateDatabase = false;
                        mb.ExportInfo.ExportTableStructure = true;
                        mb.ExportInfo.ExportRows = false;
                        try
                        {
                            mb.ExportToFile(_fileNameSql);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Can't create struct dump");
                            Console.WriteLine("Details :{0}", e.Message);
                            return false;
                        }
                    }
                }
              
            }

            using (
                MySqlConnection conn =
                    new MySqlConnection(ConfigurationManager.ConnectionStrings["DefaultContext"].ConnectionString))
            {
                using (MySqlCommand cmd = new MySqlCommand())
                {
                    using (MySqlBackup mb = new MySqlBackup(cmd))
                    {
                        cmd.Connection = conn;
                        try
                        {
                            conn.Open();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Can't connect to MySQL server");
                            Console.WriteLine("Details :{0}", e.Message);
                            return false;
                        }

                        mb.ExportInfo.AddCreateDatabase = false;
                        mb.ExportInfo.ExportTableStructure = false;
                        mb.ExportInfo.ExportRows = true;
                        try
                        {
                            mb.ExportToFile(_fileNameData);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Can't create dump");
                            Console.WriteLine("Details :{0}", e.Message);
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private static bool SshUpload(Uri url)
        {
            Console.WriteLine("Select SSH method");

            using (var sftp = new SftpClient(url.Host , url.IsDefaultPort?22:url.Port, ConfigurationManager.AppSettings["UserName"], ConfigurationManager.AppSettings["Password"]))
            {
                try
                {
                    sftp.Connect();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Can't connect to {0}",url);
                    Console.WriteLine("Details :{0}", e.Message);
                    return false;
                }

                using (var file = File.OpenRead(_fileNameZip))
                {
                    Console.WriteLine("Uploading...");
                    sftp.UploadFile(file,
                        String.Format("{0}{1}", ConfigurationManager.AppSettings["RemotePath"],
                            Path.GetFileName(_fileNameZip)), true);
                }
                Console.WriteLine("Disconnect");
                sftp.Disconnect();
            }

            return true;
        }

        private static bool FtpUpload(Uri url)
        {
            Console.WriteLine("Select FTP method");
            var fi = new FileInfo(_fileNameZip);

            var ftpClient = (FtpWebRequest)FtpWebRequest.Create(url.AbsoluteUri+ "/"+fi.Name);
            if (ConfigurationManager.AppSettings["UserName"] != null)
            {
                ftpClient.Credentials = new NetworkCredential(ConfigurationManager.AppSettings["UserName"],
                    ConfigurationManager.AppSettings["Password"]);
            }
            ftpClient.Method = WebRequestMethods.Ftp.UploadFile;
            ftpClient.UseBinary = true;
            ftpClient.KeepAlive = true;
            
            ftpClient.ContentLength = fi.Length;
            var buffer = new byte[4097];
            var totalBytes = (int)fi.Length;

            using (var fs = fi.OpenRead())
            using (var rs = ftpClient.GetRequestStream())
            {
                while (totalBytes > 0)
                {
                    var bytes = fs.Read(buffer, 0, buffer.Length);
                    rs.Write(buffer, 0, bytes);
                    totalBytes = totalBytes - bytes;
                }
            }

            try
            {
                using (var uploadResponse = (FtpWebResponse) ftpClient.GetResponse())
                {
                    Console.WriteLine("Upload status code {0} {1}", (int)uploadResponse.StatusCode ,uploadResponse.StatusDescription); 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Can't upload file");
                Console.WriteLine("Details :{0}", e.Message);
                return false;
            }
            Console.WriteLine("Disconnect");
            return true;


        }

    }
}
