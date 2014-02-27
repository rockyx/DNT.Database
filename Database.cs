using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNT.Database
{
  abstract class Database
  {
    DbConnection source;
    DbConnection target;
    DbCommand queryText;
    DbCommand createText;
    DbCommand insertText;

    public Database(DbConnection source, DbConnection target)
    {
      this.source = source;
      this.target = target;

      queryText = Source.CreateCommand();
      createText = Target.CreateCommand();
      insertText = Target.CreateCommand();
    }

    protected DbConnection Source
    {
      get { return source; }
    }

    protected DbConnection Target
    {
      get { return target; }
    }

    protected DbCommand QueryText
    {
      get { return queryText; }
      set { queryText = value; }
    }

    protected DbCommand CreateText
    {
      get { return createText; }
      set { createText = value; }
    }

    protected DbCommand InsertText
    {
      get { return insertText; }
      set { insertText = value; }
    }

    public abstract void CopyTo();
  }
}
