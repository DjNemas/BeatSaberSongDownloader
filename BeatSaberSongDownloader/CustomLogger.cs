using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeatSaberSongDownloader
{
    internal static class CustomLogger
    {
        private static Dictionary<uint, string> _customLinesDic = new();
        private static Action _updatedDicValue = OnUpdatedDicValue;
        private static object _blocker = new object();
        private static int? _customLineCursorTop = null;

        public static async Task ErrorWriteLineAsync(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            await Console.Out.WriteLineAsync($"[Error] {msg}");
            Console.ResetColor();
        }

        public static async Task DebugWriteLineAsync(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            await Console.Out.WriteLineAsync($"[Debug] {msg}");
            Console.ResetColor();
        }

        public static async Task InfoWriteLineAsync(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            await Console.Out.WriteLineAsync($"[Info] {msg}");
            Console.ResetColor();
        }

        public static async Task InfoWriteAsync(string msg, int? cursorLeft = null)
        {
            if (cursorLeft is not null)
                Console.CursorLeft = (int)cursorLeft;
            Console.ForegroundColor = ConsoleColor.Cyan;
            await Console.Out.WriteAsync($"[Info] {msg}");
            Console.ResetColor();
        }

        public static void ResetCustomLines()
        {
            _customLinesDic.Clear();
            _customLineCursorTop = null;
        }

        public static void InfoCustomLines(uint position, string msg)
        {
            lock (_blocker)
            {
                if(_customLinesDic.ContainsKey(position))
                    _customLinesDic[position] = $"[Info] {msg}";
                else
                    _customLinesDic.Add(position, $"[Info] {msg}");
            }
            _updatedDicValue.Invoke();
        }

        public static void ErrorCustomLines(uint position, string msg)
        {
            lock (_blocker)
            {
                if (_customLinesDic.ContainsKey(position))
                    _customLinesDic[position] = $"[Error] {msg}";
                else
                    _customLinesDic.Add(position, $"[Error] {msg}");
            }
            _updatedDicValue.Invoke();
        }

        public static void WriteCustomLines()
        {

            lock(_blocker)
            {
                var sb = new StringBuilder();
                sb.Capacity = Int32.MaxValue / 16;
                IOrderedEnumerable<KeyValuePair<uint, string>> orderedList;
                orderedList = _customLinesDic.OrderBy(k => k.Key);

                foreach (var msg in orderedList)
                {
                    sb.AppendLine(msg.Value);
                }

                if (_customLineCursorTop is null)
                    _customLineCursorTop = Console.CursorTop;
                else
                    Console.CursorTop = (int)_customLineCursorTop;

                Console.CursorLeft = 0;
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(sb.ToString());
                Console.ResetColor();
            }
        }

        private static void OnUpdatedDicValue()
        {
            WriteCustomLines();
        }
    }
}
