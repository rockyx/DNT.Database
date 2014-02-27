using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace DNT.Database
{
  class CryptoVehicleTroubleCode : CryptoDatabase
  {
    public CryptoVehicleTroubleCode(DbConnection source, DbConnection target, string[] classes)
      : base(source, target)
    {
      if (classes == null || classes.Length == 0 || classes[0] == "")
      {
        QueryText.CommandText = "SELECT [ID], [Code], [Content], [Description], [Language], [Class] FROM [TroubleCode]";
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
        QueryText.CommandText = command.ToString();
      }

      CreateText.CommandText = "CREATE TABLE [TroubleCode] ([ID] int primary key, [Code] blob, [Content] blob, [Description], [Language] blob, [Class] blob)";
      InsertText.CommandText = "INSERT INTO [TroubleCode] ([ID], [Code], [Content], [Description], [Language], [Class]) VALUES(@p1, @p2, @p3, @p4, @p5, @p6)";
      InsertText.CommandType = CommandType.Text;

      DbParameter param = InsertText.CreateParameter();
      param.DbType = DbType.Int32;
      param.ParameterName = "@p1";
      InsertText.Parameters.Add(param);

      for (int i = 2; i < 7; i++)
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
          InsertText.Parameters[4].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(4));
          InsertText.Parameters[5].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(5));
          InsertText.ExecuteNonQuery();
        }

        ts.Commit();
      }
    }
  }
}
