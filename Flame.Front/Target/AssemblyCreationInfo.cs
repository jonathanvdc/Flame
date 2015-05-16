using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public struct AssemblyCreationInfo
    {
        public AssemblyCreationInfo(string Name, Version Version, Lazy<bool> IsExecutable)
        {
            this = default(AssemblyCreationInfo);
            this.Name = Name;
            this.Version = Version;
            this.isExec = IsExecutable;
        }
        public AssemblyCreationInfo(string Name, Version Version, bool IsExecutable)
        {
            this = default(AssemblyCreationInfo);
            this.Name = Name;
            this.Version = Version;
            this.isExec = new Lazy<bool>(() => IsExecutable);
        }

        public string Name { get; private set; }
        public Version Version { get; private set; }

        private Lazy<bool> isExec;

        public bool IsExecutable
        {
            get
            {
                return isExec.Value;
            }
        }
    }
}
