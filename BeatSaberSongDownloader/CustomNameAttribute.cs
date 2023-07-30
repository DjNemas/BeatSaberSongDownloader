using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberSongDownloader
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal class CustomNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public CustomNameAttribute(string name)
        {
            this.Name = name;
        }

        public string GetName() => Name;
    }
}
