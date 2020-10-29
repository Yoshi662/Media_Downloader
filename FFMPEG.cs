using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Media_Downloader
{
    public class FFMPEG
    {
        private String FFMPEGFile;
        public FFMPEG(string FFMPEGfile)
        {
            this.FFMPEGFile = FFMPEGfile;
        }
        public void Convert(string path, string extensionSeleccionada)
        {
            while (!File.Exists(path))
            {
                Thread.Sleep(1000);
            }
            String[] rutasOrigen = File.ReadAllText(path).Split('\n');

            Parallel.ForEach(rutasOrigen, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (s, options) => {
                if (!String.IsNullOrWhiteSpace(s))
                {
                    String input = s;
                    String output = s.Substring(0, s.Length - s.Split('.').Last().Count()) + extensionSeleccionada;

                    if (!File.Exists(s))
                    {
                        input = s.Substring(0, s.Length - s.Split('.').Last().Count()) + "mkv";
                    }

                    if (input == output)
                    {
                        options.Break();
                        return;
                    }

                    Process pr = new Process();
                    pr.StartInfo = new ProcessStartInfo
                    {
                        FileName = FFMPEGFile,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    //4audio ffmpeg.exe -y -i 'INPUT' -f FORMATO -vn -c:v copy 'OUTPUT' 
                    //4video .\ffmpeg.exe -y -i 'INPUT' -map -c:v copy 'OUTPUT'

                    pr.StartInfo.Arguments = "-y -i \"" + input + "\" -c:v copy \"" + output + "\"";
                    pr.Start();
                    pr.WaitForExit();
                    if(pr.ExitCode != 0)
                    {
                        pr.StartInfo.Arguments = "-y -i \"" + input + "\" \"" + output + "\"";
                        pr.Start();
                        pr.WaitForExit();
                    }
                    pr.Dispose();
                    File.Delete(input);
                }
            });
            File.Delete(path);
        }
    }
}
