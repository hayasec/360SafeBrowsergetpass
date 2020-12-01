using System;
using System.Collections;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;


namespace Safe360Browsergetpass
{
    public class SQLiteBase
    {
        private IntPtr database;
        private const int SQL_OK = 0;
        private const int SQL_ROW = 100;
        private const int SQL_DONE = 101;
        public SQLiteBase()
        {
            database = IntPtr.Zero;
        }
        public SQLiteBase(String baseName, String key)
        {
            OpenDatabase(baseName, key);
        }
        public void OpenDatabase(String baseName, String key)
        {
            if (sqlite3_open(StringToPointer(baseName), out database) != SQL_OK)
            {
                database = IntPtr.Zero;
                throw new Exception("Error with opening database " + baseName + "!");
            }
            byte[] passWord = null;
            int keyLength = 0;
            if (!string.IsNullOrEmpty(key))
            {
                passWord = Encoding.UTF8.GetBytes(key);
                keyLength = passWord.Length;
            }
            sqlite3_key(database, passWord, keyLength);
        }

        private IntPtr StringToPointer(String str)
        {
            if (str == null)
            {
                return IntPtr.Zero;
            }
            else
            {
                Encoding encoding = Encoding.UTF8;
                Byte[] bytes = encoding.GetBytes(str);
                int length = bytes.Length + 1;
                IntPtr pointer = HeapAlloc(GetProcessHeap(), 0, (UInt32)length);
                Marshal.Copy(bytes, 0, pointer, bytes.Length);
                Marshal.WriteByte(pointer, bytes.Length, 0);
                return pointer;
            }
        }
        public void CloseDatabase()
        {
            if (database != IntPtr.Zero)
            {
                sqlite3_close(database);
            }
        }

        public ArrayList GetTables()
        {
            String query = "SELECT name FROM sqlite_master " +
                                        "WHERE type IN ('table','view') AND name NOT LIKE 'sqlite_%'" +
                                        "UNION ALL " +
                                        "SELECT name FROM sqlite_temp_master " +
                                        "WHERE type IN ('table','view') " +
                                        "ORDER BY 1";
            DataTable table = ExecuteQuery(query);

            ArrayList list = new ArrayList();
            foreach (DataRow row in table.Rows)
            {
                list.Add(row.ItemArray[0].ToString());
            }
            return list;
        }
        public DataTable ExecuteQuery(String query)
        {
            IntPtr statement;
            IntPtr excessData;
            sqlite3_prepare_v2(database, StringToPointer(query), GetPointerLenght(StringToPointer(query)), out statement, out excessData);
            DataTable table = new DataTable();
            int result = ReadFirstRow(statement, ref table);

            while (result == SQL_ROW)
                result = ReadNextRow(statement, ref table);

            sqlite3_finalize(statement);
            return table;
        }

        public void ExecuteNonQuery(String query)
        {
            IntPtr error;
            sqlite3_exec(database, StringToPointer(query), IntPtr.Zero, IntPtr.Zero, out error);
            if (error != IntPtr.Zero)
                throw new Exception("Error with executing non-query: \"" + query + "\"!\n" + PointerToString(sqlite3_errmsg(error)));
        }
        public enum SQLiteDataTypes
        {
            INT = 1,
            FLOAT,
            TEXT,
            BLOB,
            NULL
        };
        private int ReadFirstRow(IntPtr statement, ref DataTable table)
        {
            table = new DataTable("resultTable");
            int resultType = sqlite3_step(statement);

            if (resultType == SQL_ROW)
            {
                int columnCount = sqlite3_column_count(statement);
                String columnName = "";
                int columnType = 0;
                object[] columnValues = new object[columnCount];

                for (int i = 0; i < columnCount; i++)
                {
                    columnName = PointerToString(sqlite3_column_name(statement, i));
                    columnType = sqlite3_column_type(statement, i);

                    switch (columnType)
                    {
                        case (int)SQLiteDataTypes.INT:
                            {
                                table.Columns.Add(columnName, Type.GetType("System.Int32"));
                                columnValues[i] = sqlite3_column_int(statement, i);
                                break;
                            }
                        case (int)SQLiteDataTypes.FLOAT:
                            {
                                table.Columns.Add(columnName, Type.GetType("System.Single"));
                                columnValues[i] = sqlite3_column_double(statement, i);
                                break;
                            }
                        case (int)SQLiteDataTypes.TEXT:
                            {
                                table.Columns.Add(columnName, Type.GetType("System.String"));
                                columnValues[i] = PointerToString(sqlite3_column_text(statement, i));
                                break;
                            }
                        case (int)SQLiteDataTypes.BLOB:
                            {
                                table.Columns.Add(columnName, Type.GetType("System.String"));
                                columnValues[i] = PointerToString(sqlite3_column_blob(statement, i));
                                break;
                            }
                        default:
                            {
                                table.Columns.Add(columnName, Type.GetType("System.String"));
                                columnValues[i] = "";
                                break;
                            }
                    }
                }

                table.Rows.Add(columnValues);
            }
            return sqlite3_step(statement);
        }
        private int ReadNextRow(IntPtr statement, ref DataTable table)
        {
            int columnCount = sqlite3_column_count(statement);

            int columnType = 0;
            object[] columnValues = new object[columnCount];

            for (int i = 0; i < columnCount; i++)
            {
                columnType = sqlite3_column_type(statement, i);

                switch (columnType)
                {
                    case (int)SQLiteDataTypes.INT:
                        {
                            columnValues[i] = sqlite3_column_int(statement, i);
                            break;
                        }
                    case (int)SQLiteDataTypes.FLOAT:
                        {
                            columnValues[i] = sqlite3_column_double(statement, i);
                            break;
                        }
                    case (int)SQLiteDataTypes.TEXT:
                        {
                            columnValues[i] = PointerToString(sqlite3_column_text(statement, i));
                            break;
                        }
                    case (int)SQLiteDataTypes.BLOB:
                        {
                            columnValues[i] = PointerToString(sqlite3_column_blob(statement, i));
                            break;
                        }
                    default:
                        {
                            columnValues[i] = "";
                            break;
                        }
                }
            }
            table.Rows.Add(columnValues);
            return sqlite3_step(statement);
        }
        private String PointerToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;

            Encoding encoding = Encoding.UTF8;

            int length = GetPointerLenght(ptr);
            Byte[] bytes = new Byte[length];
            Marshal.Copy(ptr, bytes, 0, length);
            return encoding.GetString(bytes, 0, length);
        }

        private int GetPointerLenght(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return 0;
            return lstrlen(ptr);
        }
        #region DLL_Active
        [DllImport("kernel32")]
        private extern static IntPtr HeapAlloc(IntPtr heap, UInt32 flags, UInt32 bytes);

        [DllImport("kernel32")]
        private extern static IntPtr GetProcessHeap();

        [DllImport("kernel32")]
        private extern static int lstrlen(IntPtr str);

        [DllImport("sqlite3.dll")]
        private static extern int sqlite3_open(IntPtr fileName, out IntPtr database);

        [DllImport("sqlite3.dll", EntryPoint = "sqlite3_key", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_key(IntPtr db, byte[] key, int keylen);

        [DllImport("sqlite3.dll")]
        private static extern int sqlite3_close(IntPtr database);

        [DllImport("sqlite3.dll")]
        private static extern int sqlite3_exec(IntPtr database, IntPtr query, IntPtr callback, IntPtr arguments, out IntPtr error);

        [DllImport("sqlite3.dll")]
        private static extern IntPtr sqlite3_errmsg(IntPtr database);

        [DllImport("sqlite3.dll")]
        private static extern int sqlite3_prepare_v2(IntPtr database, IntPtr query, int length, out IntPtr statement, out IntPtr tail);

        [DllImport("sqlite3.dll")]
        private static extern int sqlite3_step(IntPtr statement);

        [DllImport("sqlite3.dll")]
        private static extern int sqlite3_column_count(IntPtr statement);

        [DllImport("sqlite3.dll")]
        private static extern IntPtr sqlite3_column_name(IntPtr statement, int columnNumber);

        [DllImport("sqlite3.dll")]
        private static extern int sqlite3_column_type(IntPtr statement, int columnNumber);

        [DllImport("sqlite3.dll")]
        private static extern int sqlite3_column_int(IntPtr statement, int columnNumber);

        [DllImport("sqlite3.dll")]
        private static extern double sqlite3_column_double(IntPtr statement, int columnNumber);

        [DllImport("sqlite3.dll")]
        private static extern IntPtr sqlite3_column_text(IntPtr statement, int columnNumber);

        [DllImport("sqlite3.dll")]
        private static extern IntPtr sqlite3_column_blob(IntPtr statement, int columnNumber);

        [DllImport("sqlite3.dll")]
        private static extern IntPtr sqlite3_column_table_name(IntPtr statement, int columnNumber);

        [DllImport("sqlite3.dll")]
        private static extern int sqlite3_finalize(IntPtr handle);
        #endregion DLL_Active
    }
}