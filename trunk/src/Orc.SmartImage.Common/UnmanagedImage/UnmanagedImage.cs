﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace Orc.SmartImage
{
    public abstract class UnmanagedImage<T> : IDisposable, IImage
        where T : struct
    {
        public Int32 ByteCount { get; private set; }
        public Int32 Length { get; private set; }
        public Int32 SizeOfType { get; private set; }

        public Int32 Width { get; private set; }
        public Int32 Height { get; private set; }

        public IntPtr StartIntPtr { get; private set; }

        private IColorConverter m_converter;
        private unsafe Byte* m_start;

        public unsafe UnmanagedImage(Int32 width, Int32 height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException("width");
            else if (height <= 0) throw new ArgumentOutOfRangeException("height");
            Height = height;
            Width = width;
            Length = Width * Height;
            SizeOfType = SizeOfT();
            ByteCount = SizeOfType * Length;
            m_converter = this.CreateByteConverter();
            StartIntPtr = Marshal.AllocHGlobal(ByteCount);
            m_start = (Byte*)StartIntPtr;
        }

        public UnmanagedImage(Bitmap map):this(map.Width, map.Height)
        {
            if (map == null) throw new ArgumentNullException("map");
            this.CreateFromBitmap(map);
        }

        public unsafe void CloneFrom(UnmanagedImage<T> src)
        {
            if(src == null) throw new ArgumentNullException("src");
            if(src.ByteCount != this.ByteCount) throw new NotSupportedException("与src图像的像素数量不一致，无法复制.");

            Byte* start = m_start;
            Byte* end = m_start + ByteCount;

            Byte* from = src.m_start;

            while (start != end)
            {
                *start = *from;
                ++start;
                ++from;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (false == disposed)
            {
                 disposed = true;
                 Marshal.FreeHGlobal(StartIntPtr);
            }
        }

        private bool disposed;

        ~UnmanagedImage()
        {
            Dispose(false);
        }

        private static Int32 SizeOfT()
        {
            return Marshal.SizeOf(typeof(T));
        }

        private unsafe void CreateFromBitmap(Bitmap map)
        {
            int height = map.Height;
            int width = map.Width;
            PixelFormat format = map.PixelFormat;

            if (this.Width != width || this.Height != height)
            {
                return;
            }

            Bitmap newMap = map;
            Int32 step = SizeOfT();

            switch (format)
            {
                case PixelFormat.Format24bppRgb:
                    break;
                case PixelFormat.Format32bppArgb:
                    break;
                default:
                    format = PixelFormat.Format32bppArgb;
                    newMap = map.Clone(new Rectangle(0, 0, width, height), PixelFormat.Format32bppArgb);
                    break;
            }

            BitmapData data = newMap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, format);
            Byte* line = (Byte*)data.Scan0;
            Byte* dstLine = (Byte*)StartIntPtr;
            try
            {
                if (format == PixelFormat.Format24bppRgb)
                {
                    for (int h = 0; h < height; h++)
                    {
                        m_converter.Copy((Rgb24*)line, (void*)dstLine, width);
                        line += data.Stride;
                        dstLine += step * width;
                    }
                }
                else
                {
                    for (int h = 0; h < height; h++)
                    {
                        m_converter.Copy((Argb32*)line, (void*)dstLine, width);

                        line += data.Stride;
                        dstLine += step * width;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                newMap.UnlockBits(data);
                if (newMap != map)
                {
                    newMap.Dispose();
                }
            }
        }

        public virtual unsafe Bitmap ToBitmap()
        {
            Bitmap map = new Bitmap(this.Width, this.Height, GetOutputBitmapPixelFormat());
            ToBitmap(map);
            return map;
        }

        public virtual unsafe void ToBitmap(Bitmap map)
        {
            if (map == null) throw new ArgumentNullException("map");
            if (map.Width != this.Width || map.Height != this.Height)
            {
                throw new ArgumentException("尺寸不匹配.");
            }

            if (map.PixelFormat != GetOutputBitmapPixelFormat())
            {
                throw new ArgumentException("只支持 " + GetOutputBitmapPixelFormat().ToString() + " 格式。 ");
            }

            Int32 step = SizeOfT();
            Byte* srcLine = (Byte*)StartIntPtr;

            BitmapData data = map.LockBits(new Rectangle(0, 0, map.Width, map.Height), ImageLockMode.ReadWrite, map.PixelFormat);
            try
            {
                int width = map.Width;
                int height = map.Height;
                Byte* dstLine = (Byte*)data.Scan0;
                for (int h = 0; h < height; h++)
                {
                    ToBitmapCore(srcLine, dstLine, width);
                    dstLine += data.Stride;
                    srcLine += step * width;
                }
            }
            finally
            {
                map.UnlockBits(data);
            }
        }

        public void ApplyMatrix(float a, float b, float c, float d, float e, float f)
        {
            //TODO: ApplyMatrix
            throw new NotImplementedException();
        }

        public void Resize(UnmanagedImage<T> dst)
        {
            //TODO: Resize
            throw new NotImplementedException();
        }

        protected abstract PixelFormat GetOutputBitmapPixelFormat();

        protected abstract unsafe void ToBitmapCore(byte* src, byte* dst, int width);

        public abstract IImage Clone();

        protected abstract IColorConverter CreateByteConverter();
    }
}