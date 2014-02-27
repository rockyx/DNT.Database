using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNT.Database
{
  class CryptoVehicleLiveData : CryptoDatabase
  {
    public CryptoVehicleLiveData(DbConnection source, DbConnection target, string[] classes)
      : base(source, target)
    {
      if (classes == null || classes.Length == 0 || classes[0] == "")
      {
        QueryText.CommandText = ("SELECT [ID], [ShortName], [Content], [Unit], [DefaultValue], [CommandName], [CommandClass], [Description], [Language], [Class], [Index] FROM [LiveData]");
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
        QueryText.CommandText = command.ToString();
      }

      CreateText.CommandText = string.Format("CREATE TABLE [LiveData] ([ID] int primary key, [ShortName] blob, [Content] blob, [Unit] blob, [DefaultValue] blob, [CommandName] blob, [CommandClass] blob, [Description] blob, [Language] blob, [Class] blob, [Index] blob)");
      InsertText.CommandText = "INSERT INTO [LiveData] ([ID], [ShortName], [Content], [Unit], [DefaultValue], [CommandName], [CommandClass], [Description], [Language], [Class], [Index]) VALUES(@p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11)";

      DbParameter param = InsertText.CreateParameter();
      param.DbType = DbType.Int32;
      param.ParameterName = "@p1";
      InsertText.Parameters.Add(param);

      for (int i = 2; i < 12; i++)
      {
        param = InsertText.CreateParameter();
        param.DbType = DbType.Binary;
        param.ParameterName = string.Format("@p{0}", i);
        InsertText.Parameters.Add(param);
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
          InsertText.Parameters[2].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(2));
          if (string.IsNullOrEmpty(result.GetFieldValue<string>(3)))
          {
            InsertText.Parameters[3].Value = DBNull.Value;
          }
          else
          {
            InsertText.Parameters[3].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(3));
          }

          if (string.IsNullOrEmpty(result.GetFieldValue<string>(4)))
          {
            InsertText.Parameters[4].Value = DBNull.Value;
          }
          else
          {
            InsertText.Parameters[4].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(4));
          }

          if (string.IsNullOrEmpty(result.GetFieldValue<string>(5)))
          {
            InsertText.Parameters[5].Value = DBNull.Value;
          }
          else
          {
            InsertText.Parameters[5].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(5));
          }

          if (string.IsNullOrEmpty(result.GetFieldValue<string>(6)))
          {
            InsertText.Parameters[6].Value = DBNull.Value;
          }
          else
          {
            InsertText.Parameters[6].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(6));
          }

          if (string.IsNullOrEmpty(result.GetFieldValue<string>(7)))
          {
            InsertText.Parameters[7].Value = DBNull.Value;
          }
          else
          {
            InsertText.Parameters[7].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(7));
          }
          InsertText.Parameters[8].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(8));
          InsertText.Parameters[9].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(9));
          InsertText.Parameters[10].Value = Crypto.EncryptBytesToBytes(BitConverter.GetBytes(result.GetFieldValue<int>(10)));
          InsertText.ExecuteNonQuery();
        }

        ts.Commit();
      }
    }
  }
}
