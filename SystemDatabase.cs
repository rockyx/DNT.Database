using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNT.Database
{
  class SystemDatabase : Database
  {
    public SystemDatabase(DbConnection Source, DbConnection target)
      : base(Source, target)
    {
    }

    public override void CopyTo()
    {
      throw new NotImplementedException();
    }
  }
}
