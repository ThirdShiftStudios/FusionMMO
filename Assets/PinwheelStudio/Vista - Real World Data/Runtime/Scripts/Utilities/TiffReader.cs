#if VISTA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pinwheel.Vista;
using Pinwheel.Vista.Graphics;
using BitMiracle.LibTiff.Classic;
using System.IO;

namespace Pinwheel.Vista.RealWorldData
{
    public static class TiffReader
    {
        public class ReadTileFailedException : System.Exception
        {
            public ReadTileFailedException(string msg) : base(msg)
            {
            }
        }

        public enum BitDepth
        {
            Bit16 = 16,
            Bit32 = 32
        }

        public enum Pivot
        {
            BottomLeft,
            TopLeft
        }

        public class Result
        {
            public int width;
            public int height;
            public byte[] data;
        }

        private static readonly string DUMMY_TIFF_NAME = "Downloaded Tiff";
        private static readonly string TIFF_READ_MODE = "r";

        public static Result Read(byte[] bytes, BitDepth bitDepth, Pivot pivot)
        {
            Result result = new Result();
            Stream stream = new MemoryStream(bytes);
            TiffStream tiffStream = new TiffStream();

            Tiff tiff = Tiff.ClientOpen(DUMMY_TIFF_NAME, TIFF_READ_MODE, stream, tiffStream);

            int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
            int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

            result.width = width;
            result.height = height;

            int lineSize = tiff.ScanlineSize();
            byte[] tiffData = new byte[lineSize * height];

            if (tiff.IsTiled())
            {
                int tileWidth = tiff.GetField(TiffTag.TILEWIDTH)[0].ToInt();
                int tileHeight = tiff.GetField(TiffTag.TILELENGTH)[0].ToInt();
                int tileCountX = Mathf.CeilToInt(width * 1.0f / tileWidth);
                int tileCountY = Mathf.CeilToInt(height * 1.0f / tileHeight);

                byte[] tileBytes = new byte[tiff.TileSize()];
                for (int tileX = 0; tileX < tileCountX; ++tileX)
                {
                    for (int tileY = 0; tileY < tileCountY; ++tileY)
                    {
                        int readResult = tiff.ReadTile(tileBytes, 0, tileX, tileY, 0, 0);
                        if (readResult < 0)
                        {
                            throw new ReadTileFailedException("Failed to read tiff file: Read Tile error");
                        }

                        CopyTileDataToTiffData(tileBytes, tileX, tileY, tileWidth, tileHeight, tiffData, width, height, bitDepth, pivot);
                    }
                }
            }
            else
            {
                for (int row = 0; row < height; ++row)
                {
                    int offset = row * lineSize;
                    if (!tiff.ReadScanline(tiffData, offset, row, 0))
                    {
                        throw new System.Exception("Failed to read tiff file: Read Scanline error");
                    }
                }
            }

            tiffStream.Close(stream); //this will also close the stream object
            tiff.Close();

            result.data = tiffData;

            return result;
        }

        private static void CopyTileDataToTiffData(byte[] tileData, int tileX, int tileY, int tileWidth, int tileHeight, byte[] tiffData, int imageWidth, int imageHeight, BitDepth bitDepth, Pivot pivot)
        {
            int pixelOriginX = tileX * tileWidth;
            int pixelOriginY = (pivot == Pivot.BottomLeft) ? (tileY * tileHeight) : (imageHeight - 1 - tileY * tileHeight);
            for (int x = 0; x < tileWidth; ++x)
            {
                for (int y = 0; y < tileHeight; ++y)
                {
                    int pixelX = pixelOriginX + x;
                    int pixelY = (pivot == Pivot.BottomLeft) ? (pixelOriginY + y) : (pixelOriginY - y);
                    if (pixelX < 0 || 
                        pixelY < 0 ||
                        pixelX >= imageWidth || 
                        pixelY >= imageHeight)
                        continue;
                    if (bitDepth == BitDepth.Bit16)
                    {
                        int tileDataOffset = ((y * tileWidth) + x) * 2;
                        int tiffDataOffset = ((pixelY * imageWidth) + pixelX) * 2;

                        tiffData[tiffDataOffset + 0] = tileData[tileDataOffset + 0];
                        tiffData[tiffDataOffset + 1] = tileData[tileDataOffset + 1];
                    }
                    else if (bitDepth == BitDepth.Bit32)
                    {
                        int tileDataOffset = ((y * tileWidth) + x) * 4;
                        int tiffDataOffset = ((pixelY * imageWidth) + pixelX) * 4;

                        tiffData[tiffDataOffset + 0] = tileData[tileDataOffset + 0];
                        tiffData[tiffDataOffset + 1] = tileData[tileDataOffset + 1];
                        tiffData[tiffDataOffset + 2] = tileData[tileDataOffset + 2];
                        tiffData[tiffDataOffset + 3] = tileData[tileDataOffset + 3];
                    }
                }
            }
        }

    }
}
#endif
