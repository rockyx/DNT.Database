using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNT.Database
{
  abstract class CryptoDatabase : Database
  {
    public CryptoDatabase(DbConnection source, DbConnection target)
      : base(source, target)
    {
    }
  }
}
