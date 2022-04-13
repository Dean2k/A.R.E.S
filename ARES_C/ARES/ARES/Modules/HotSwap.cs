using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ARES.Modules
{
    public static class HotSwap
    {
        public static void DecompressToFile(BundleFileInstance bundleInst, string savePath, HotswapConsole hotSwap)
        {
            AssetBundleFile bundle = bundleInst.file;
            SafeWrite(hotSwap.txtStatusText, $"22.2% Bundle file assigned!" + Environment.NewLine);
            SafeProgress(hotSwap.pbProgress, 22);
            FileStream bundleStream = File.Open(savePath, FileMode.Create);
            SafeWrite(hotSwap.txtStatusText, $"33.3% Loaded file to bundle stream!" + Environment.NewLine);
            SafeProgress(hotSwap.pbProgress, 33);
            var progressBar = new SZProgress(hotSwap);
            bundle.Unpack(bundle.reader, new AssetsFileWriter(bundleStream), progressBar);
            SafeWrite(hotSwap.txtStatusText, $"44.4% Unpack stream complete!" + Environment.NewLine);
            SafeProgress(hotSwap.pbProgress, 44);
            bundleStream.Position = 0;
            SafeWrite(hotSwap.txtStatusText, $"55.5% Bundle stream position assigned!" + Environment.NewLine);
            SafeProgress(hotSwap.pbProgress, 55);
            AssetBundleFile newBundle = new AssetBundleFile();
            SafeWrite(hotSwap.txtStatusText, $"66.6% Created new asset bundle file!" + Environment.NewLine);
            SafeProgress(hotSwap.pbProgress, 66);
            newBundle.Read(new AssetsFileReader(bundleStream), false);
            SafeWrite(hotSwap.txtStatusText, $"77.7% Bundle written to file!" + Environment.NewLine);
            SafeProgress(hotSwap.pbProgress, 77);
            bundle.reader.Close();
            SafeWrite(hotSwap.txtStatusText, $"88.8% Bundle closed!" + Environment.NewLine);
            SafeProgress(hotSwap.pbProgress, 88);
            bundleInst.file = newBundle;
            bundleStream.Flush();
            bundleStream.Close();
            SafeWrite(hotSwap.txtStatusText, $"100% Bundle instance cleaned!" + Environment.NewLine);
            SafeProgress(hotSwap.pbProgress, 100);
        }
        //Creates function allowing it to be used with string imputs
        public static void DecompressToFileStr(string bundlePath, string unpackedBundlePath, HotswapConsole hotSwap)
        {
            var am = new AssetsManager();
            SafeWrite(hotSwap.txtStatusText, "11.1% Declared new asset manager!" + Environment.NewLine);
            SafeProgress(hotSwap.pbProgress, 11);
            DecompressToFile(am.LoadBundleFile(bundlePath), unpackedBundlePath, hotSwap);
        }

        private static void SafeWrite(TextBox text, string textWrite)
        {
            if (text.InvokeRequired)
            {
                text.Invoke((MethodInvoker)delegate
                {
                    text.Text += textWrite;
                });
            }
        }

        private static void SafeProgress(ProgressBar progress, int value)
        {
            if (progress.InvokeRequired)
            {
                progress.Invoke((MethodInvoker)delegate
                {
                    progress.Value = value;
                });
            }
        }



        //Creates function to compress asset bundles
        public static void CompressBundle(string file, string compFile, HotswapConsole hotSwap)
        {
            using (var stream = new AssetsFileWriter(compFile))
            {
                var am = new AssetsManager();
                SafeWrite(hotSwap.txtStatusText, $"25% Declared new asset manager!" + Environment.NewLine);
                var bun = am.LoadBundleFile(file, false);
                SafeWrite(hotSwap.txtStatusText, $"50% Bundle file initialized!" + Environment.NewLine);
                var progressBar = new SZProgress(hotSwap);
                bun.file.Pack(bun.file.reader, stream, AssetBundleCompressionType.LZMA, progressBar);
                SafeWrite(hotSwap.txtStatusText, $"100% Compressed file packing complete!" + Environment.NewLine);
                am.UnloadAll();
                bun = null;
            }

        }


    }
}
