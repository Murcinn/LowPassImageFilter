﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Drawing.Imaging;


using System.Windows.Media.Imaging;

using JaProjektFiltr.Filter;
using JaProjektFiltr.Extension;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Net.Http;

namespace JaProjektFiltr
{
    enum ChooseDLL
    {
        Assembly,
        Cpp
    }

    internal class Program
    {
        private BitmapSource _oldBitmap;


        private List<Interface> programInterface = new List<Interface>();
        private List<Task> _tasks = new List<Task>();
        private int _numberOfThreads;
        private byte[] _allPixels;

        const int _bitsInByte = 8;






        public static Interface Create(ChooseDLL languageLevel, int bytesPerPixel, int startIndex, int endIndex)
        {
            switch (languageLevel)
            {
                case ChooseDLL.Assembly:
                    return new AssemblyFilter(bytesPerPixel, startIndex, endIndex);
                case ChooseDLL.Cpp:
                    return new CppFilter(bytesPerPixel, startIndex, endIndex);
                default:
                    return null;
            }
        }

        public Program(BitmapSource bitmapImage, ChooseDLL languageLevel, int numberOfThreads)
        {
            _oldBitmap = bitmapImage;
            _allPixels = ReclaimPixels(bitmapImage);
            _numberOfThreads = numberOfThreads;
            int partLenght = AdjustPieceLenght();

            for (int partNumber = 0; partNumber < _numberOfThreads; partNumber++)
            {
                int tempPartNumber = partNumber;
                int partEnd;
                if (partNumber + 1 == _numberOfThreads)
                    partEnd = _allPixels.Length;
                else
                    partEnd = partLenght * (tempPartNumber + 1) - 1;

                programInterface.Add(Create(languageLevel, bitmapImage.Format.BitsPerPixel / _bitsInByte, partLenght * tempPartNumber, partEnd));
                _tasks.Add(new Task(() => programInterface[tempPartNumber].ExecuteResult(_allPixels)));
            }
        }

        private int AdjustPieceLenght()
        {

            int partLenght = _allPixels.Length / _numberOfThreads;
            while (partLenght % (_oldBitmap.Format.BitsPerPixel / _bitsInByte) != 0)
                partLenght++;
            return partLenght;
        }

        private byte[] ReclaimPixels(BitmapSource bitmapImage)
        {
            return bitmapImage.ConvertToBmpArrayBGR();
        }

        public BitmapSource RunProgram(out System.TimeSpan elapsedTime)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Parallel.ForEach(_tasks, (task) => task.Start());
            Task.WaitAll(_tasks.ToArray());

            stopwatch.Stop();
            elapsedTime = stopwatch.Elapsed;

            return _allPixels.ConvertBmpArrayBGRToImageFloat(_oldBitmap.PixelWidth, _oldBitmap.PixelHeight, _oldBitmap.Format);
        }

        private void SaveImageToDisk(BitmapSource image, string filePath)
        {

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(fileStream);
            }

        }


        static void Main(string[] args)
        {




        BitmapSource _newBitmap = new BitmapImage(new System.Uri("F:\\GitHub\\Gradientowy-filtr-kierunkowy-wschodni\\Temp\\bmpPath.bmp"));
        BitmapSource _newBitmap1 = new BitmapImage(new System.Uri("F:\\GitHub\\Gradientowy-filtr-kierunkowy-wschodni\\Temp\\bmpPath.bmp"));


            Program progCpp = new Program(_newBitmap, ChooseDLL.Cpp, 1);

            BitmapSource resCpp = progCpp.RunProgram(out System.TimeSpan elapsedTimeCpp);

            progCpp.SaveImageToDisk(resCpp, "F:\\GitHub\\Gradientowy-filtr-kierunkowy-wschodni\\Temp\\bmpCppOut.bmp");

            Console.Write("Czas wykonywania programu: " + elapsedTimeCpp + "\n\n");




            Program progAsm = new Program(_newBitmap1, ChooseDLL.Assembly, 1);

            BitmapSource resAsm = progAsm.RunProgram(out System.TimeSpan elapsedTimeAsm);

            progAsm.SaveImageToDisk(resAsm, "F:\\GitHub\\Gradientowy-filtr-kierunkowy-wschodni\\Temp\\bmpAsmOut.bmp");

            Console.Write("Czas wykonywania programu: " + elapsedTimeAsm + "\n\n");

            //Console.ReadLine();
            Environment.Exit(0);



        }
    }
}
