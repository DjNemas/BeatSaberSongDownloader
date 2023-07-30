using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberSongDownloader
{
    public static class EnumExtensions
    {
        public static T GetAttribute<T>(this Enum value) where T : Attribute => 
            value.GetType().GetField(value.ToString()).GetCustomAttributes<T>(false).SingleOrDefault();
    }
}
