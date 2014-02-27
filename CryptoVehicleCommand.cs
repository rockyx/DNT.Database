using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNT.Database
{
  class CryptoVehicleCommand : CryptoDatabase
  {
    public CryptoVehicleCommand(DbConnection source, DbConnection target, string[] classes)
      : base(source, target)
    {
      CreateText.CommandText = "CREATE TABLE Command ([ID] int primary key, [Name] blob, [Command] blob, [Class] blob)";
      InsertText.CommandText = "INSERT INTO Command ([ID], [Name], [Command], [Class]) VALUES(@p1, @p2, @p3, @p4)";

      DbParameter param = InsertText.CreateParameter();
      param.DbType = DbType.Int32;
      param.ParameterName = "@p1";
      InsertText.Parameters.Add(param);

      for (int i = 2; i < 5; i++)
      {
        param = InsertText.CreateParameter();
        param.DbType = DbType.Binary;
        param.ParameterName = string.Format("@p{0}", i);
        InsertText.Parameters.Add(param);
      }

      if (classes == null || classes.Length == 0 || classes[0] == "")
      {
        QueryText.CommandText = ("SELECT [ID], [Name], [Command], [Class] FROM [Command]");
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
        QueryText.CommandText = command.ToString();
      }
    }

    public override void CopyTo()
    {
      using (var result = QueryText.ExecuteReader())
      {
        DbTransaction ts = Target.BeginTransaction();

        CreateText.ExecuteNonQuery();

        while (result.Read())
        {
          InsertText.Parameters[0].Value = result.GetFieldValue<int>(0);
          InsertText.Parameters[1].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(1));
          byte[] command = result.GetFieldValue<byte[]>(2);
          //byte[] cmd = new byte[command.Length + 2];
          //cmd[0] = (byte)((command.Length >> 8) & 0xFF);
          //cmd[1] = (byte)(command.Length & 0xFF);
          //Array.Copy(command, 0, cmd, 2, command.Length);
          //InsertText.Parameters[2].Value = Crypto.EncryptBytesToBytes(cmd);
          InsertText.Parameters[2].Value = Crypto.EncryptBytesToBytes(command);
          InsertText.Parameters[3].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(3));
          InsertText.ExecuteNonQuery();
        }

        ts.Commit();
      }
    }
  }
}
