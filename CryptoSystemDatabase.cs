using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNT.Database
{
  class CryptoSystemDatabase : CryptoDatabase
  {
    public CryptoSystemDatabase(DbConnection source, DbConnection target)
      : base(source, target)
    {
      QueryText.CommandText = "SELECT [ID], [Name], [Content], [Language] FROM [Text] WHERE [Class] = 'System'";
      CreateText.CommandText = string.Format("CREATE TABLE [Text] ([ID] int primary key, [Name] blob, [Content] blob, [Language] blob)");
      InsertText.CommandText = "INSERT INTO [Text] ([ID], [Name], [Content], [Language]) VALUES (@p1, @p2, @p3, @p4)";
      InsertText.CommandType = CommandType.Text;

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
          InsertText.Parameters[3].Value = Crypto.EncryptStringToBytes(result.GetFieldValue<string>(3));
          InsertText.ExecuteNonQuery();
        }
        ts.Commit();

      }
    }
  }
}
