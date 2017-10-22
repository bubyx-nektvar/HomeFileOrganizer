using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HomeFileOrganizer.Classes;
using HomeFileOrganizer.Classes.Interfaces;

namespace HomeFileOrganizer.Managers
{
    class Searcher
    {
        Classes.HomeData data;
        public Searcher (HomeData d)
        {
            data = d;
        }
        /// <summary>
        /// Analyze search string and find corresponding files of folders.
        /// </summary>
        /// <param name="searchPattern">
        /// String that specifie search fiter.
        /// String don't support brackets. only avitable logical operator are & and |, when & has higher priotity than |.
        /// </param>
        public Task<List<Task<IInfoGetter>>> Search(string searchPattern)
        {
            return Task<List<Task<IInfoGetter>>>.Run<List<Task<IInfoGetter>>>(() =>
            {
                string[] splitedOr = searchPattern.Split('|');
                var root = new OrSetOp();
                foreach (var orPart in splitedOr)
                {
                    var and = new AndSetOp();
                    root.AddOperand(and);

                    var splitedAnd = orPart.Split('&');
                    foreach (var andPart in splitedAnd)
                    {
                        and.AddOperand(Filter.Parse(andPart));
                    }
                }
                //fitrace
                List<Task<IInfoGetter>> l = new List<Task<IInfoGetter>>();
                foreach (IInfoGetter i in data)
                {
                    l.Add(Filterate(root, i));
                }
                return l;
            });
        }
        /// <summary>
        /// Determine if <paramref name="item"/> is coresponding with filre <paramref name="root"/>.
        /// </summary>
        /// <param name="root">Root of operation tree, that determine if <paramref name="item"/> is corresponding to searched file.</param>
        /// <returns>Item if pass filter, otherwise null.</returns>
        private Task<IInfoGetter> Filterate(Operation root,IInfoGetter item)
        {
            return Task<IInfoGetter>.Run<IInfoGetter>(() =>
            {
                if (root.DetermineOperation(item)) return item;
                else return null;
            });
        }
    }
    interface IOperable
    {
        bool DetermineOperation(IInfoGetter i);
    }
    abstract class Operation:IOperable
    {
        protected List<IOperable> operands=new List<IOperable>();

        public void AddOperand(IOperable i)
        {
            operands.Add(i);
        }
        public abstract bool DetermineOperation(IInfoGetter i);
    }
    class AndSetOp : Operation
    {
        public override bool DetermineOperation(IInfoGetter i)
        {
            bool res = true;
            foreach(var o in operands)
            {
                res = res && o.DetermineOperation(i);
            }
            return res;
        }
    }
    class OrSetOp : Operation
    {
        public override bool DetermineOperation(IInfoGetter i)
        {
            bool res = false;
            foreach(var o in operands)
            {
                res = res | o.DetermineOperation(i);
            }
            return res;
        }
    }
    /// <summary>
    /// Operation that compare data in infoFile with filer option.
    /// </summary>
    class Filter:IOperable
    {
        public string infoItemPath;
        public string value;
        public bool ZeroAvitable=false;
        public bool negativeAvitable=false;
        public bool positiveAvitable=false;
        public bool DetermineOperation(IInfoGetter info)
        {
            Item i = info.GetInfoFile().GetItem(infoItemPath);
            if (i != null)
            {
                int x = i.CompareTo(value);
                if (x == 0 && ZeroAvitable) return true;
                else if (x < 0 && negativeAvitable) return true;
                else if (x > 0 && positiveAvitable) return true;
                else return false;
            }
            else return false;
        }

        /// <summary>
        /// Parse filter operation.
        /// </summary>
        /// <param name="s">{infoPath}{comparands}{value}, do not solve '"'</param>
        /// <returns></returns>
        internal static Filter Parse(string s)
        {
            var f = new Filter();
            var ss = s.Split(new char[] { '<', '>', '=' },StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length == 2)
            {
                var opers = s.Substring(ss[0].Length, s.Length - ss[0].Length - ss[1].Length);
                f.infoItemPath = ss[0];
                f.value = ss[1];
                foreach(char c in opers)
                {
                    switch (c)
                    {
                        case '<':
                            f.negativeAvitable = true;
                            break;
                        case '>':
                            f.positiveAvitable = true;
                            break;
                        case '=':
                            f.ZeroAvitable = true;
                            break;
                        default:
                            throw new FormatException();
                    }
                }
                return f;
            }
            else throw new FormatException();
        }
    }
}
