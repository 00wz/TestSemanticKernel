using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TestSemanticKernel
{
    internal static class ConsoleEncoding
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleCP(uint wCodePageID);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        public static void UseUtf8()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    // В Windows для интерактивного ввода через ConPTY (VS Code/Windows Terminal)
                    // безопаснее использовать UTF-16LE (Encoding.Unicode), иначе возможны \u0000 в строке.
                    // Для вывода используем UTF-8.
                    SetConsoleOutputCP(65001); // 65001 = UTF-8 (только для вывода)

                    // Вывод: UTF-8 без BOM
                    Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

                    // Ввод:
                    // - если ввод перенаправлен (pipe/файл), используем UTF-8
                    // - если интерактивный терминал (ConPTY), используем UTF-16LE
                    if (Console.IsInputRedirected)
                    {
                        Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    }
                    else
                    {
                        Console.InputEncoding = Encoding.Unicode; // UTF-16LE
                    }
                }
                else
                {
                    // На *nix обычно и ввод, и вывод в UTF-8
                    Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                    Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                }
            }
            catch
            {
                // Ignore any encoding setup errors; we'll proceed with defaults.
            }
        }
    }
}