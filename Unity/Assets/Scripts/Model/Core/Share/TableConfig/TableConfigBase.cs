using System.Collections.Generic;

namespace ET
{
    [EnableClass]
    public class TableConfigBase<T> where T : Luban.BeanBase
    {
        public virtual T Get(int id)
        {
            return null;
        }
        
        public virtual T GetOrDefault(int id)
        {
            return null;
        }

        public virtual List<T> GetDataList()
        {
            return null;
        }
        
        public virtual int GetRowCount()
        {
            return 0;
        }
    }
}