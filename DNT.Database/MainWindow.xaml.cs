using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using System.Data.SQLite;
using System.Data.SqlClient;
using System.Data.Common;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.IO;

namespace DNT.Database
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private byte[] key = { 0x12, 0x89, 0xF8, 0x47, 0x5F, 0x6C, 0x16, 0x93, 0x8C, 0x75, 0x4F, 0xFA, 0x33, 0xDC, 0x81, 0x3C, 0xCE, 0x4C, 0x34, 0x92, 0x00, 0xB7, 0x3A, 0x38 };
        //private byte[] iv = { 0x34, 0xD0, 0xB7, 0x8A, 0x6A, 0x4A, 0x8E, 0xD3 };
        private byte[] key = 
        { 
            0xFA, 0xC2, 0xCC, 0x82, 
            0x8C, 0xFD, 0x42, 0x17, 
            0xA0, 0xB2, 0x97, 0x4D, 
            0x19, 0xC8, 0xA4, 0xB1, 
            0xF5, 0x73, 0x23, 0x7C, 
            0xB1, 0xC4, 0xC0, 0x38, 
            0xC9, 0x80, 0xB9, 0xF7, 
            0xC3, 0x3E, 0xC9, 0x12 
        }; // For AES
        private byte[] iv = 
        { 
            0x7C, 0xF4, 0xF0, 0x7D, 
            0x3B, 0x0D, 0xA1, 0xC6, 
            0x35, 0x74, 0x18, 0xB3, 
            0x51, 0xA3, 0x87, 0x8E 
        }; // For AES
        private VehicleDB vds;
        private VehicleDBTableAdapters.TableAdapterManager vdam;
        private SqlConnection sqlconn;
        private string[] language = { "zh-CN", "en-US" };
        private Dictionary<byte[], byte[]> byteMap = new Dictionary<byte[], byte[]>(new ByteArrayComparer());
        public MainWindow()
        {
            InitializeComponent();
            vds = new VehicleDB();
            vdam = new VehicleDBTableAdapters.TableAdapterManager();

            try
            {
                sqlconn = new SqlConnection(Properties.Settings.Default.Vehicle_Scanner_DatabaseConnectionString);
                sqlconn.Open();

                vdam.Connection = sqlconn;
                vdam.TextTableAdapter = new VehicleDBTableAdapters.TextTableAdapter();
                vdam.TextTableAdapter.Connection = sqlconn;
                vdam.TroubleCodeTableAdapter = new VehicleDBTableAdapters.TroubleCodeTableAdapter();
                vdam.TroubleCodeTableAdapter.Connection = sqlconn;
                vdam.LiveDataTableAdapter = new VehicleDBTableAdapters.LiveDataTableAdapter();
                vdam.LiveDataTableAdapter.Connection = sqlconn;
                vdam.CommandTableAdapter = new VehicleDBTableAdapters.CommandTableAdapter();
                vdam.CommandTableAdapter.Connection = sqlconn;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnButtonOpenDatabase(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsEnabled = false;
                Task.Factory.StartNew(() =>
                {
                    vds.Reset();

                    vdam.TextTableAdapter.Fill(vds.Text);
                    vdam.TroubleCodeTableAdapter.Fill(vds.TroubleCode);
                    vdam.LiveDataTableAdapter.Fill(vds.LiveData);
                    vdam.CommandTableAdapter.Fill(vds.Command);
                }).ContinueWith((t) =>
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.IsEnabled = true;
                        if (t.IsFaulted)
                        {
                            MessageBox.Show(t.Exception.InnerException.Message);
                        }
                        else
                        {
                            textDG.ItemsSource = vds.Text.DefaultView;
                            troubleCodeDG.ItemsSource = vds.TroubleCode.DefaultView;
                            liveDataDG.ItemsSource = vds.LiveData.DefaultView;
                            commandDG.ItemsSource = vds.Command.DefaultView;
                        }
                    }));
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OnButtonUpdate(object sender, RoutedEventArgs e)
        {
            try
            {
                vdam.UpdateAll(vds);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        [DllImport("dntdatabase", EntryPoint = "Encrypt", CallingConvention = CallingConvention.Cdecl)]
        private static extern uint Encrypt(byte[] buff, uint count, byte[] outbuff);

        private byte[] EncryptBytesToBytes(byte[] plainBytes)
        {
            // Check arguments
            if (plainBytes == null || plainBytes.Length <= 0)
                throw new ArgumentException("plainBytes");

            if (byteMap.ContainsKey(plainBytes))
                return byteMap[plainBytes];

            // Return the encrypted bytes from the memory stream.
            int length = plainBytes.Length;
            int times = length / 16 + 1;
            int size = times * 16;
            byte[] encrypted = new byte[size];
            Encrypt(plainBytes, (uint)plainBytes.Length, encrypted);
            return encrypted;
        }

        private byte[] EncryptStringToBytes(string plainText)
        {
            List<byte> textUTF8 = new List<byte>();
            byte[] plainBuff = UTF8Encoding.UTF8.GetBytes(plainText);
            textUTF8.AddRange(plainBuff);
            byte[] encrypt = EncryptBytesToBytes(textUTF8.ToArray());
            return encrypt;
        }

        private static byte[] DecryptBytesFromBytes(byte[] cipherBytes, byte[] key, byte[] iv)
        {
            // Check arguments
            if (cipherBytes == null || cipherBytes.Length <= 0)
                throw new ArgumentException("cipherBytes");
            if (key == null || key.Length <= 0)
                throw new ArgumentException("Key");
            if (iv == null || iv.Length <= 0)
                throw new ArgumentException("IV");

            // Declare the bytes used to hold
            // the decrypted text.
            List<byte> plainbytes = new List<byte>();
            byte[] buff = new byte[0x1000];

            // Create an TripleDESCryptoServiceProvider object
            // with the specified key and IV.
            //using (TripleDESCryptoServiceProvider tdsAlg = new TripleDESCryptoServiceProvider())
            using (Aes alg = Aes.Create())
            {
                alg.Key = key;
                alg.IV = iv;
                alg.Padding = PaddingMode.Zeros;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = alg.CreateDecryptor(alg.Key, alg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (BinaryReader brDecrypt = new BinaryReader(csDecrypt, Encoding.UTF8))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a bytes
                            for (int len = brDecrypt.Read(buff, 0, buff.Length); len > 0; len = brDecrypt.Read(buff, 0, buff.Length))
                            {
                                for (int i = 0; i < len; ++i)
                                {
                                    plainbytes.Add(buff[i]);
                                }
                            }
                        }
                    }
                }
                return plainbytes.ToArray();
            }
        }

        private static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            // Check arguments
            if (cipherText == null || cipherText.Length <= 0)
                //throw new ArgumentException("cipherText");
                return "";
            if (key == null || key.Length <= 0)
                throw new ArgumentException("Key");
            if (iv == null || iv.Length <= 0)
                throw new ArgumentException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an TripleDESCryptoServiceProvider object
            // with the specified key and IV.
            //using (TripleDESCryptoServiceProvider tdsAlg = new TripleDESCryptoServiceProvider())
            using (Aes alg = Aes.Create())
            {
                alg.Key = key;
                alg.IV = iv;
                alg.Padding = PaddingMode.Zeros;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = alg.CreateDecryptor(alg.Key, alg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {
                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        private void OnButtonSaveSystem(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "DB files (*.db)|*.db|All Files(*.*)|*.*";
            dlg.ShowDialog();

            string dbFile = dlg.FileName;
            if (string.IsNullOrEmpty(dbFile))
                throw new IOException(dbFile);

            this.IsEnabled = false;

            Task.Factory.StartNew(() =>
            {
                SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbFile);

                try
                {
                    conn.Open();

                    SqlCommand queryText = new SqlCommand();
                    SQLiteCommand createText = new SQLiteCommand();
                    SQLiteCommand insertText = new SQLiteCommand();

                    queryText.Connection = sqlconn;
                    createText.Connection = conn;
                    insertText.Connection = conn;

                    queryText.CommandText = "SELECT [ID], [Name], [Content], [Language] FROM [Text] WHERE [Class] = 'System'";
                    createText.CommandText = string.Format("CREATE TABLE [Text] ([ID] int primary key, [Name] blob, [Content] blob, [Language] blob)");
                    insertText.CommandText = "INSERT INTO [Text] ([ID], [Name], [Content], [Language]) VALUES (@p1, @p2, @p3, @p4)";
                    insertText.CommandType = CommandType.Text;
                    insertText.Parameters.Add(new SQLiteParameter("@p1", DbType.Int32));//, 0, "ID", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
                    insertText.Parameters.Add(new SQLiteParameter("@p2", DbType.Binary));//, 0, "Name", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
                    insertText.Parameters.Add(new SQLiteParameter("@p3", DbType.Binary));//, 0, "Content", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
                    insertText.Parameters.Add(new SQLiteParameter("@p4", DbType.Binary));//, 0, "Language", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));

                    SQLiteTransaction ts = conn.BeginTransaction();

                    createText.ExecuteNonQuery();
                    var result = queryText.ExecuteReader();
                    while (result.Read())
                    {
                        insertText.Parameters[0].Value = result.GetFieldValue<int>(0);
                        insertText.Parameters[1].Value = EncryptStringToBytes(result.GetFieldValue<string>(1));
                        insertText.Parameters[2].Value = EncryptStringToBytes(result.GetFieldValue<string>(2));
                        insertText.Parameters[3].Value = EncryptStringToBytes(result.GetFieldValue<string>(3));
                        insertText.ExecuteNonQuery();
                    }

                    ts.Commit();
                }
                finally
                {
                    conn.Close();
                }
            }).ContinueWith((t) =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.IsEnabled = true;
                }));
            });
        }

        private void VehicleTextSave(SQLiteConnection conn, string[] classes)
        {
            SqlCommand queryText = new SqlCommand();
            SQLiteCommand createText = new SQLiteCommand();
            SQLiteCommand insertText = new SQLiteCommand();

            queryText.Connection = sqlconn;
            createText.Connection = conn;
            insertText.Connection = conn;

            if (classes == null || classes.Length == 0 || classes[0] == "")
            {
                queryText.CommandText = ("SELECT [ID], [Name], [Content], [Language], [Class] FROM [Text]");
            }
            else
            {
                StringBuilder command = new StringBuilder();
                command.Append("SELECT [ID], [Name], [Content], [Language], [Class] FROM [Text] WHERE ");
                for (int i = 0; i < classes.Length; i++)
                {
                    if (i != 0)
                        command.Append(" OR ");
                    command.Append("[Class] = \'");
                    command.Append(classes[i]);
                    command.Append('\'');
                }
                queryText.CommandText = command.ToString();
            }
            createText.CommandText = string.Format("CREATE TABLE [Text] ([ID] int primary key, [Name] blob, [Content] blob, [Language] blob, [Class] blob)");
            insertText.CommandText = "INSERT INTO [Text] ([ID], [Name], [Content], [Language], [Class]) VALUES (@p1, @p2, @p3, @p4, @p5)";
            insertText.CommandType = CommandType.Text;
            insertText.Parameters.Add(new SQLiteParameter("@p1", DbType.Int32));//, 0, "ID", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertText.Parameters.Add(new SQLiteParameter("@p2", DbType.Binary));//, 0, "Name", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertText.Parameters.Add(new SQLiteParameter("@p3", DbType.Binary));//, 0, "Content", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertText.Parameters.Add(new SQLiteParameter("@p4", DbType.Binary));//, 0, "Language", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertText.Parameters.Add(new SQLiteParameter("@p5", DbType.Binary));//, 0, "Class", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));

            createText.ExecuteNonQuery();
            var result = queryText.ExecuteReader();

            while (result.Read())
            {
                insertText.Parameters[0].Value = result.GetFieldValue<int>(0);
                insertText.Parameters[1].Value = EncryptStringToBytes(result.GetFieldValue<string>(1));
                insertText.Parameters[2].Value = EncryptStringToBytes(result.GetFieldValue<string>(2));
                insertText.Parameters[3].Value = EncryptStringToBytes(result.GetFieldValue<string>(3));
                insertText.Parameters[4].Value = EncryptStringToBytes(result.GetFieldValue<string>(4));
                insertText.ExecuteNonQuery();
            }
            result.Close();
        }

        private void TroubleCodeSave(SQLiteConnection conn, string[] classes)
        {
            SqlCommand queryText = new SqlCommand();
            SQLiteCommand createTroubleCode = new SQLiteCommand();
            SQLiteCommand insertTroubleCode = new SQLiteCommand();

            queryText.Connection = sqlconn;
            createTroubleCode.Connection = conn;
            insertTroubleCode.Connection = conn;

            if (classes == null || classes.Length == 0 || classes[0] == "")
            {
                queryText.CommandText = ("SELECT [ID], [Code], [Content], [Description], [Language], [Class] FROM [TroubleCode]");
            }
            else
            {
                StringBuilder command = new StringBuilder();
                command.Append("SELECT [ID], [Code], [Content], [Description], [Language], [Class] FROM [TroubleCode] WHERE ");
                for (int i = 0; i < classes.Length; i++)
                {
                    if (i != 0)
                        command.Append(" OR ");
                    command.Append("[Class] = \'");
                    command.Append(classes[i]);
                    command.Append('\'');
                }
                queryText.CommandText = command.ToString();
            }

            createTroubleCode.CommandText = string.Format("CREATE TABLE [TroubleCode] ([ID] int primary key, [Code] blob, [Content] blob, [Description], [Language] blob, [Class] blob)");
            insertTroubleCode.CommandText = "INSERT INTO [TroubleCode] ([ID], [Code], [Content], [Description], [Language], [Class]) VALUES(@p1, @p2, @p3, @p4, @p5, @p6)";
            insertTroubleCode.CommandType = CommandType.Text;
            insertTroubleCode.Parameters.Add(new SQLiteParameter("@p1", DbType.Int32));//, 0, "ID", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertTroubleCode.Parameters.Add(new SQLiteParameter("@p2", DbType.Binary));//, 0, "Code", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertTroubleCode.Parameters.Add(new SQLiteParameter("@p3", DbType.Binary));//, 0, "Content", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertTroubleCode.Parameters.Add(new SQLiteParameter("@p4", DbType.Binary));//, 0, "Description", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertTroubleCode.Parameters.Add(new SQLiteParameter("@p5", DbType.Binary));//, 0, "Language", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertTroubleCode.Parameters.Add(new SQLiteParameter("@p6", DbType.Binary));//, 0, "Class", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));

            createTroubleCode.ExecuteNonQuery();
            var result = queryText.ExecuteReader();
            while (result.Read())
            {
                insertTroubleCode.Parameters[0].Value = result.GetFieldValue<int>(0);
                insertTroubleCode.Parameters[1].Value = EncryptStringToBytes(result.GetFieldValue<string>(1));
                insertTroubleCode.Parameters[2].Value = EncryptStringToBytes(result.GetFieldValue<string>(2));
                if (string.IsNullOrEmpty(result.GetFieldValue<string>(3)))
                {
                    insertTroubleCode.Parameters[3].Value = DBNull.Value;
                }
                else
                {
                    insertTroubleCode.Parameters[3].Value = EncryptStringToBytes(result.GetFieldValue<string>(3));
                }
                insertTroubleCode.Parameters[4].Value = EncryptStringToBytes(result.GetFieldValue<string>(4));
                insertTroubleCode.Parameters[5].Value = EncryptStringToBytes(result.GetFieldValue<string>(5));
                insertTroubleCode.ExecuteNonQuery();
            }
            result.Close();
        }

        private void LiveDataSave(SQLiteConnection conn, string[] classes)
        {
            SqlCommand queryText = new SqlCommand();
            SQLiteCommand createLiveData = new SQLiteCommand();
            SQLiteCommand insertLiveData = new SQLiteCommand();

            queryText.Connection = sqlconn;
            createLiveData.Connection = conn;
            insertLiveData.Connection = conn;

            if (classes == null || classes.Length == 0 || classes[0] == "")
            {
                queryText.CommandText = ("SELECT [ID], [ShortName], [Content], [Unit], [DefaultValue], [CommandName], [CommandClass], [Description], [Language], [Class], [Index] FROM [LiveData]");
            }
            else
            {
                StringBuilder command = new StringBuilder();
                command.Append("SELECT [ID], [ShortName], [Content], [Unit], [DefaultValue], [CommandName], [CommandClass], [Description], [Language], [Class], [Index] FROM [LiveData] WHERE ");
                for (int i = 0; i < classes.Length; i++)
                {
                    if (i != 0)
                        command.Append(" OR ");
                    command.Append("[Class] = \'");
                    command.Append(classes[i]);
                    command.Append('\'');
                }
                queryText.CommandText = command.ToString();
            }
            createLiveData.CommandText = string.Format("CREATE TABLE [LiveData] ([ID] int primary key, [ShortName] blob, [Content] blob, [Unit] blob, [DefaultValue] blob, [CommandName] blob, [CommandClass] blob, [Description] blob, [Language] blob, [Class] blob, [Index] blob)");
            insertLiveData.CommandText = "INSERT INTO [LiveData] ([ID], [ShortName], [Content], [Unit], [DefaultValue], [CommandName], [CommandClass], [Description], [Language], [Class], [Index]) VALUES(@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11)";
            insertLiveData.Parameters.Add(new SQLiteParameter("@p1", DbType.Int32));//, 0, "ID", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p2", DbType.Binary));//, 0, "ShortName", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p3", DbType.Binary));//, 0, "Content", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p4", DbType.Binary));//, 0, "Unit", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p5", DbType.Binary));//, 0, "DefaultValue", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p6", DbType.Binary));//, 0, "CommandName", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p7", DbType.Binary));//, 0, "CommandClass", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p8", DbType.Binary));//, 0, "Description", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p9", DbType.Binary));//, 0, "Language", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p10", DbType.Binary));//, 0, "Class", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertLiveData.Parameters.Add(new SQLiteParameter("@p11", DbType.Binary));

            createLiveData.ExecuteNonQuery();
            var result = queryText.ExecuteReader();
            while (result.Read())
            {
                insertLiveData.Parameters[0].Value = result.GetFieldValue<int>(0);
                insertLiveData.Parameters[1].Value = EncryptStringToBytes(result.GetFieldValue<string>(1));
                insertLiveData.Parameters[2].Value = EncryptStringToBytes(result.GetFieldValue<string>(2));
                if (string.IsNullOrEmpty(result.GetFieldValue<string>(3)))
                {
                    insertLiveData.Parameters[3].Value = DBNull.Value;
                }
                else
                {
                    insertLiveData.Parameters[3].Value = EncryptStringToBytes(result.GetFieldValue<string>(3));
                }

                if (string.IsNullOrEmpty(result.GetFieldValue<string>(4)))
                {
                    insertLiveData.Parameters[4].Value = DBNull.Value;
                }
                else
                {
                    insertLiveData.Parameters[4].Value = EncryptStringToBytes(result.GetFieldValue<string>(4));
                }

                if (string.IsNullOrEmpty(result.GetFieldValue<string>(5)))
                {
                    insertLiveData.Parameters[5].Value = DBNull.Value;
                }
                else
                {
                    insertLiveData.Parameters[5].Value = EncryptStringToBytes(result.GetFieldValue<string>(5));
                }

                if (string.IsNullOrEmpty(result.GetFieldValue<string>(6)))
                {
                    insertLiveData.Parameters[6].Value = DBNull.Value;
                }
                else
                {
                    insertLiveData.Parameters[6].Value = EncryptStringToBytes(result.GetFieldValue<string>(6));
                }

                if (string.IsNullOrEmpty(result.GetFieldValue<string>(7)))
                {
                    insertLiveData.Parameters[7].Value = DBNull.Value;
                }
                else
                {
                    insertLiveData.Parameters[7].Value = EncryptStringToBytes(result.GetFieldValue<string>(7));
                }
                insertLiveData.Parameters[8].Value = EncryptStringToBytes(result.GetFieldValue<string>(8));
                insertLiveData.Parameters[9].Value = EncryptStringToBytes(result.GetFieldValue<string>(9));
                insertLiveData.Parameters[10].Value = EncryptBytesToBytes(BitConverter.GetBytes(result.GetFieldValue<int>(10)));
                insertLiveData.ExecuteNonQuery();
            }
            result.Close();
        }

        private void CommandSave(SQLiteConnection conn, string[] classes)
        {
            SqlCommand queryText = new SqlCommand();
            SQLiteCommand createCommand = new SQLiteCommand();
            SQLiteCommand insertCommand = new SQLiteCommand();

            queryText.Connection = sqlconn;
            createCommand.Connection = conn;
            insertCommand.Connection = conn;

            createCommand.CommandText = "CREATE TABLE Command ([ID] int primary key, [Name] blob, [Command] blob, [Class] blob)";
            insertCommand.CommandText = "INSERT INTO Command ([ID], [Name], [Command], [Class]) VALUES(@p1, @p2, @p3, @p4)";
            insertCommand.Parameters.Add(new SQLiteParameter("@p1", DbType.Int32));//, 0, "ID", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertCommand.Parameters.Add(new SQLiteParameter("@p2", DbType.Binary));//, 0, "Name", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertCommand.Parameters.Add(new SQLiteParameter("@p3", DbType.Binary));//, 0, "Command", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));
            insertCommand.Parameters.Add(new SQLiteParameter("@p4", DbType.Binary));//, 0, "Class", ParameterDirection.Input, true, 0, 0, DataRowVersion.Current, null));

            if (classes == null || classes.Length == 0 || classes[0] == "")
            {
                queryText.CommandText = ("SELECT [ID], [Name], [Command], [Class] FROM [Command]");
            }
            else
            {
                StringBuilder command = new StringBuilder();
                command.Append("SELECT [ID], [Name], [Command], [Class] FROM [Command] WHERE ");
                for (int i = 0; i < classes.Length; i++)
                {
                    if (i != 0)
                        command.Append(" OR ");
                    command.Append("[Class] = \'");
                    command.Append(classes[i]);
                    command.Append('\'');
                }
                queryText.CommandText = command.ToString();
            }

            createCommand.ExecuteNonQuery();
            var result = queryText.ExecuteReader();
            while (result.Read())
            {
                insertCommand.Parameters[0].Value = result.GetFieldValue<int>(0);
                insertCommand.Parameters[1].Value = EncryptStringToBytes(result.GetFieldValue<string>(1));
                byte[] command = result.GetFieldValue<byte[]>(2);
                byte[] cmd = new byte[command.Length + 2];
                cmd[0] = (byte)((command.Length >> 8) & 0xFF);
                cmd[1] = (byte)(command.Length & 0xFF);
                Array.Copy(command, 0, cmd, 2, command.Length);
                insertCommand.Parameters[2].Value = EncryptBytesToBytes(cmd);
                insertCommand.Parameters[3].Value = EncryptStringToBytes(result.GetFieldValue<string>(3));
                insertCommand.ExecuteNonQuery();
            }
            result.Close();
        }

        private void OnButtonSaveVehicle(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "DB files (*.db)|*.db|All Files(*.*)|*.*";
            dlg.ShowDialog();

            string dbFile = dlg.FileName;
            if (string.IsNullOrEmpty(dbFile))
                throw new IOException(dbFile);

            this.IsEnabled = false;
            string textClassText = textClass.Text;
            string troubleCodeClassText = troubleClass.Text;
            string liveDataClassText = liveDataClass.Text;
            string commandClassText = commandClass.Text;

            Task.Factory.StartNew(() =>
            {
                SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbFile);

                try
                {
                    conn.Open();

                    SQLiteTransaction ts = conn.BeginTransaction();
                    // Text Table
                    VehicleTextSave(conn, textClassText.Split(';'));

                    // Trouble Code Table 
                    TroubleCodeSave(conn, troubleCodeClassText.Split(';'));

                    // Live Data Table
                    LiveDataSave(conn, liveDataClassText.Split(';'));

                    // Command Table
                    CommandSave(conn, commandClassText.Split(';'));

                    ts.Commit();
                }
                finally
                {
                    conn.Close();
                }
            }).ContinueWith((t) =>
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    this.IsEnabled = true;
                }));
            });
        }

        //private void OnButtonSave(object sender, RoutedEventArgs e)
        //{
        //}

        private void OnButtonCopyFromSQLiteDB(object sender, RoutedEventArgs e)
        {
            try
            {
                this.IsEnabled = false;

                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Filter = "DB files (*.db)|*.db|All Files(*.*)|*.*";
                dlg.ShowDialog();
                string dbFile = dlg.FileName;
                if (string.IsNullOrEmpty(dbFile))
                    throw new IOException(dbFile);

                Task.Factory.StartNew(() =>
                {
                    SQLiteConnection conn = new SQLiteConnection("Data Source=" + dbFile);

                    try
                    {
                        conn.Open();

                        SQLiteCommand queryText = new SQLiteCommand();
                        SQLiteCommand queryTroubleCode = new SQLiteCommand();
                        SQLiteCommand queryLiveData = new SQLiteCommand();
                        SQLiteCommand queryCommand = new SQLiteCommand();

                        queryText.Connection = conn;
                        queryTroubleCode.Connection = conn;
                        queryLiveData.Connection = conn;
                        queryCommand.Connection = conn;

                        queryText.CommandText = "SELECT * FROM [Text]";
                        queryTroubleCode.CommandText = "SELECT * FROM [TroubleCode]";
                        queryLiveData.CommandText = "SELECT * FROM [LiveData]";
                        queryCommand.CommandText = "SELECT * FROM [Command]";

                        SQLiteDataReader reader = queryText.ExecuteReader();
                        while (reader.Read())
                        {
                            byte[] name = (byte[])reader.GetValue(1);
                            byte[] content = (byte[])reader.GetValue(2);
                            byte[] language = (byte[])reader.GetValue(3);
                            byte[] cls = (byte[])reader.GetValue(4);
                            string dname = DecryptStringFromBytes(name, key, iv);
                            string dcontent = DecryptStringFromBytes(content, key, iv);
                            string dlanguage = DecryptStringFromBytes(language, key, iv);
                            string dcls = DecryptStringFromBytes(cls, key, iv);
                            vdam.TextTableAdapter.Insert(dname, dcontent, dlanguage, dcls);
                        }

                        reader = queryTroubleCode.ExecuteReader();
                        while (reader.Read())
                        {
                            byte[] code = (byte[])reader.GetValue(1);
                            byte[] content = (byte[])reader.GetValue(2);
                            byte[] description = (byte[])reader.GetValue(3);
                            byte[] language = (byte[])reader.GetValue(4);
                            byte[] cls = (byte[])reader.GetValue(5);
                            string dcode = DecryptStringFromBytes(code, key, iv);
                            string dcontent = DecryptStringFromBytes(content, key, iv);
                            string ddescription = DecryptStringFromBytes(description, key, iv);
                            string dlanguage = DecryptStringFromBytes(language, key, iv);
                            string dcls = DecryptStringFromBytes(cls, key, iv);
                            vdam.TroubleCodeTableAdapter.Insert(dcode, dcontent, ddescription, dlanguage, dcls);
                        }

                        reader = queryLiveData.ExecuteReader();
                        while (reader.Read())
                        {
                            byte[] shortName = (byte[])reader.GetValue(1);
                            byte[] content = (byte[])reader.GetValue(2);
                            object temp = reader.GetValue(3);
                            byte[] unit = null;
                            if (!(temp is System.DBNull))
                            {
                                unit = (byte[])temp;
                            }
                            temp = reader.GetValue(4);
                            byte[] defaultValue = null;
                            if (!(temp is System.DBNull))
                            {
                                defaultValue = (byte[])temp;
                            }
                            temp = reader.GetValue(5);
                            byte[] cmdName = null;
                            if (!(temp is System.DBNull))
                            {
                                cmdName = (byte[])temp;
                            }
                            temp = reader.GetValue(6);
                            byte[] cmdClass = null;
                            if (!(temp is System.DBNull))
                            {
                                cmdClass = (byte[])temp;
                            }

                            temp = reader.GetValue(7);
                            byte[] description = null;
                            if (!(temp is System.DBNull))
                            {
                                description = (byte[])temp;
                            }
                            byte[] language = (byte[])reader.GetValue(8);
                            byte[] cls = (byte[])reader.GetValue(9);
                            byte[] index = (byte[])reader.GetValue(10);
                            string dshortName = DecryptStringFromBytes(shortName, key, iv);
                            string dcontent = DecryptStringFromBytes(content, key, iv);
                            string dunit = DecryptStringFromBytes(unit, key, iv);
                            string ddefaultValue = DecryptStringFromBytes(defaultValue, key, iv);
                            string dcmdName = DecryptStringFromBytes(cmdName, key, iv);
                            string dcmdClass = DecryptStringFromBytes(cmdClass, key, iv);
                            string ddescription = DecryptStringFromBytes(description, key, iv);
                            string dlanguage = DecryptStringFromBytes(language, key, iv);
                            string dcls = DecryptStringFromBytes(cls, key, iv);
                            int dindex = BitConverter.ToInt32(DecryptBytesFromBytes(index, key, iv), 0);
                            vdam.LiveDataTableAdapter.Insert(dshortName, dcontent, dunit, ddefaultValue, dcmdName, dcmdClass, ddescription, dlanguage, dcls, dindex);
                        }

                        reader = queryCommand.ExecuteReader();
                        while (reader.Read())
                        {
                            byte[] name = (byte[])reader.GetValue(1);
                            byte[] cmd = (byte[])reader.GetValue(2);
                            byte[] cls = (byte[])reader.GetValue(3);
                            string dname = DecryptStringFromBytes(name, key, iv);
                            byte[] dcmd = DecryptBytesFromBytes(cmd, key, iv);
                            string dcls = DecryptStringFromBytes(cls, key, iv);
                            vdam.CommandTableAdapter.Insert(dname, dcmd, dcls);
                        }

                        vdam.TextTableAdapter.Fill(vds.Text);
                        vdam.TroubleCodeTableAdapter.Fill(vds.TroubleCode);
                        vdam.LiveDataTableAdapter.Fill(vds.LiveData);
                        vdam.CommandTableAdapter.Fill(vds.Command);

                        vdam.UpdateAll(vds);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    finally
                    {
                    }
                }).ContinueWith((t) =>
                {
                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        this.IsEnabled = true;
                        if (t.IsFaulted)
                        {
                            MessageBox.Show(t.Exception.InnerException.Message);
                        }
                        else
                        {
                            textDG.ItemsSource = vds.Text.DefaultView;
                            troubleCodeDG.ItemsSource = vds.TroubleCode.DefaultView;
                            liveDataDG.ItemsSource = vds.LiveData.DefaultView;
                            commandDG.ItemsSource = vds.Command.DefaultView;
                        }
                    }));
                });
            }
            catch
            {
            }
            finally
            {
            }
        }
    }
}
