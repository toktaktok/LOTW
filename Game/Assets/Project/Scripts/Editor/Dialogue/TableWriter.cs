#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using Project.Scripts.Editor.Data;

namespace Project.Scripts.Editor.Dialogue
{
    /// <summary>
    /// Table/ 의 Python 스크립트(table_edit.py, convert_table.py)를 실행합니다.
    /// Python 경로는 EditorPrefs 에 두고, 비어 있으면 %LOCALAPPDATA%/Programs/Python 의 최신 설치, 없으면 PATH 의 python 을 씁니다.
    /// </summary>
    public static class TableWriter
    {
        public static string TableRoot => Path.GetFullPath(ToolDefines.TableRootPath);

        public static string PythonPath
        {
            get => EditorPrefs.GetString(ToolDefines.PythonPathPrefKey, string.Empty);
            set => EditorPrefs.SetString(ToolDefines.PythonPathPrefKey, value);
        }

        public static string ExcelPath(string table) => Path.Combine(TableRoot, "Excel", $"{table}.xlsx");

        /// <summary>엑셀이 파일을 열고 있으면 생기는 잠금 파일(~$이름.xlsx)이 있는지.</summary>
        public static bool IsLocked(string table) => File.Exists(Path.Combine(TableRoot, "Excel", $"~${table}.xlsx"));

        public static DateTime GetWriteTime(string table) => File.GetLastWriteTimeUtc(ExcelPath(table));

        /// <summary>스크립트를 실행하고 (성공 여부, 출력)을 돌려줍니다.</summary>
        public static (bool success, string output) Run(string script, string argument = null)
        {
            var info = new ProcessStartInfo
            {
                FileName = ResolvePython(),
                Arguments = argument == null ? $"\"{script}\"" : $"\"{script}\" \"{argument}\"",
                WorkingDirectory = TableRoot,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            info.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

            try
            {
                using Process process = Process.Start(info);
                var error = process.StandardError.ReadToEndAsync();
                string output = process.StandardOutput.ReadToEnd();
                if(!process.WaitForExit(ToolDefines.PythonTimeoutMs))
                {
                    process.Kill();
                    return (false, $"{script} 가 {ToolDefines.PythonTimeoutMs / 1000}초 안에 끝나지 않았습니다.");
                }
                return (process.ExitCode == 0, (output + error.Result).Trim());
            }
            catch(Exception e)
            {
                return (false, $"Python 을 실행하지 못했습니다 ({info.FileName}): {e.Message}\n설정에서 python.exe 경로를 지정하세요. openpyxl 도 필요합니다 (pip install openpyxl).");
            }
        }

        private static string ResolvePython()
        {
            if(!string.IsNullOrEmpty(PythonPath))
                return PythonPath;

            string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ToolDefines.PythonInstallFolder);
            string installed = Directory.Exists(root)
                ? Directory.GetDirectories(root, "Python3*").OrderByDescending(d => d).Select(d => Path.Combine(d, "python.exe")).FirstOrDefault(File.Exists)
                : null;
            return installed ?? "python";
        }
    }
}
#endif
