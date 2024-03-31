using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace WindowsFormsApp1
{

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RTSS_SHARED_MEMORY_OSD_ENTRY
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szOSD;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szOSDOwner;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4096)]
        public string szOSDEx; // Доступно только в версии 2.7 и выше
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct RTSS_SHARED_MEMORY_APP_ENTRY
    {
        public uint dwProcessID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] // MAX_PATH обычно 260 в Windows
        public string szName;
        public uint dwFlags;
        public uint dwTime0;
        public uint dwTime1;
        public uint dwFrames;
        public uint dwFrameTime;
        public uint dwStatFlags;
        public uint dwStatTime0;
        public uint dwStatTime1;
        public uint dwStatFrames;
        public uint dwStatCount;
        public uint dwStatFramerateMin;
        public uint dwStatFramerateAvg;
        public uint dwStatFramerateMax;
        public uint dwOSDX;
        public uint dwOSDY;
        public uint dwOSDPixel;
        public uint dwOSDColor;
        public uint dwOSDFrame;
        public uint dwScreenCaptureFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szScreenCapturePath;
        public uint dwOSDBgndColor;
        public uint dwVideoCaptureFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szVideoCapturePath;
        public uint dwVideoFramerate;
        public uint dwVideoFramesize;
        public uint dwVideoFormat;
        public uint dwVideoQuality;
        public uint dwVideoCaptureThreads;
        public uint dwScreenCaptureQuality;
        public uint dwScreenCaptureThreads;
        public uint dwAudioCaptureFlags;
        public uint dwVideoCaptureFlagsEx;
        public uint dwAudioCaptureFlags2;
        public uint dwStatFrameTimeMin;
        public uint dwStatFrameTimeAvg;
        public uint dwStatFrameTimeMax;
        public uint dwStatFrameTimeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        public uint[] dwStatFrameTimeBuf;
        public uint dwStatFrameTimeBufPos;
        public uint dwStatFrameTimeBufFramerate;
        // Следующие поля для v2.6 и выше, используйте соответствующие структуры/типы для LARGE_INTEGER
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RTSS_SHARED_MEMORY
    {
        public uint dwSignature;
        public uint dwVersion;
        public uint dwAppEntrySize;
        public uint dwAppArrOffset;
        public uint dwAppArrSize;
        public uint dwOSDEntrySize;
        public uint dwOSDArrOffset;
        public uint dwOSDArrSize;
        public uint dwOSDFrame;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public RTSS_SHARED_MEMORY_OSD_ENTRY[] arrOSD;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public RTSS_SHARED_MEMORY_APP_ENTRY[] arrApp;
    }

    public class RTSS
    {
        // Импорт функций из kernel32.dll
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenFileMapping(uint dwDesiredAccess, bool bInheritHandle, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        const uint FILE_MAP_READ = 0x0004;

        static IntPtr hMapFile;

        public static IntPtr ReadSharedMemoryData() {

            // RTSSSharedMemoryV2 - имя shared memory, используемое RTSS.
            if (hMapFile == IntPtr.Zero)
            {
                hMapFile = OpenFileMapping(FILE_MAP_READ, false, "RTSSSharedMemoryV2");
            }

            if (hMapFile == IntPtr.Zero)
            {
                // return IntPtr.Zero;
                throw new Exception("Не удалось открыть область общей памяти.");
            }

            IntPtr pBuf = MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);
            if (pBuf == IntPtr.Zero)
            {
                // ShowError("Не удалось отобразить область общей памяти.");
                CloseHandle(hMapFile);
                hMapFile = IntPtr.Zero;
                // return IntPtr.Zero;
                throw new Exception("Не удалось отобразить область общей памяти.");
            }

            // UnmapViewOfFile(pBuf);
            // CloseHandle(hMapFile);

            return pBuf;

        }
        public static string RTSSSharedMemoryDataJson()
        {
            IntPtr ptr = ReadSharedMemoryData();
            if(ptr == IntPtr.Zero)
            {
                return null;
            }
            // Указываем, что хотим прочитать данные в структуру RTSS_SHARED_MEMORY
            // RTSS_SHARED_MEMORY sharedMemoryData = (RTSS_SHARED_MEMORY)Marshal.PtrToStructure(pBuf, typeof(RTSS_SHARED_MEMORY));
            // RTSS_SHARED_MEMORY_APP_ENTRY appEntry = Marshal.PtrToStructure<RTSS_SHARED_MEMORY_APP_ENTRY>(pBuf);
            string json = FormatRTSSSharedMemoryDataToJson(ptr);
            UnmapViewOfFile(ptr);
            return json;
        }

        public static string FormatRTSSSharedMemoryDataToJson(IntPtr pBuf)
        {
            var sharedMemory = Marshal.PtrToStructure<RTSS_SHARED_MEMORY>(pBuf);

            var osdEntries = new List<object>();
            IntPtr osdEntryPtr = new IntPtr(pBuf.ToInt64() + sharedMemory.dwOSDArrOffset);
            for (int i = 0; i < sharedMemory.dwOSDArrSize; i++)
            {
                var osdEntry = Marshal.PtrToStructure<RTSS_SHARED_MEMORY_OSD_ENTRY>(osdEntryPtr);

                // string extendedOSDText = "15:20:05\n\n<C0=008040><C1=0080C0><C2=C08080>..."; // Add full text here
                HardwareInfo hwinfo = OSDTextParser.ParseExtendedOSDText(osdEntry.szOSDEx);

                osdEntries.Add(new
                {
                    OSDText = osdEntry.szOSD,
                    OSDOwner = osdEntry.szOSDOwner,
                    ExtendedOSDText = osdEntry.szOSDEx,
                    ExtendedOSDJSON = hwinfo,
                });
                osdEntryPtr = IntPtr.Add(osdEntryPtr, Marshal.SizeOf(typeof(RTSS_SHARED_MEMORY_OSD_ENTRY)));
            }

            var jsonData = new
            {
                Signature = sharedMemory.dwSignature.ToString("X"),
                Version = sharedMemory.dwVersion,
                AppEntrySize = sharedMemory.dwAppEntrySize,
                AppArrayOffset = sharedMemory.dwAppArrOffset,
                AppArraySize = sharedMemory.dwAppArrSize,
                OSDEntrySize = sharedMemory.dwOSDEntrySize,
                OSDArrayOffset = sharedMemory.dwOSDArrOffset,
                OSDArraySize = sharedMemory.dwOSDArrSize,
                OSDFrame = sharedMemory.dwOSDFrame,
                OSD = osdEntries,
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(jsonData, options);

            /*
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            return JsonSerializer.Serialize(jsonData, options);
            */

        }

        public static string FormatRTSSSharedMemoryData(IntPtr pBuf)
        {
            // Преобразование указателя в структуру RTSS_SHARED_MEMORY
            RTSS_SHARED_MEMORY sharedMemory = Marshal.PtrToStructure<RTSS_SHARED_MEMORY>(pBuf);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Signature: {sharedMemory.dwSignature:X}");
            sb.AppendLine($"Version: {sharedMemory.dwVersion:X}");
            sb.AppendLine($"App Entry Size: {sharedMemory.dwAppEntrySize}");
            sb.AppendLine($"App Array Offset: {sharedMemory.dwAppArrOffset}");
            sb.AppendLine($"App Array Size: {sharedMemory.dwAppArrSize}");
            sb.AppendLine($"OSD Entry Size: {sharedMemory.dwOSDEntrySize}");
            sb.AppendLine($"OSD Array Offset: {sharedMemory.dwOSDArrOffset}");
            sb.AppendLine($"OSD Array Size: {sharedMemory.dwOSDArrSize}");
            sb.AppendLine($"Global OSD Frame ID: {sharedMemory.dwOSDFrame}");

            // Пример для чтения одного из элементов OSD
            if (sharedMemory.dwOSDArrSize > 0)
            {
                IntPtr osdEntryPtr = new IntPtr(pBuf.ToInt64() + sharedMemory.dwOSDArrOffset);
                for (int i = 0; i < sharedMemory.dwOSDArrSize; i++)
                {
                    RTSS_SHARED_MEMORY_OSD_ENTRY osdEntry = Marshal.PtrToStructure<RTSS_SHARED_MEMORY_OSD_ENTRY>(osdEntryPtr);
                    sb.AppendLine($"\n--------------------------------------\nOSD Text {i}: {osdEntry.szOSD}");
                    sb.AppendLine($"OSD Owner {i}: {osdEntry.szOSDOwner}");
                    // Для v2.7 и новее
                    sb.AppendLine($"Extended OSD Text {i}: {osdEntry.szOSDEx}");

                    osdEntryPtr = new IntPtr(osdEntryPtr.ToInt64() + Marshal.SizeOf(typeof(RTSS_SHARED_MEMORY_OSD_ENTRY)));
                }
            }

            return sb.ToString();
        }

        public static string FormatFullSharedMemoryData(RTSS_SHARED_MEMORY_APP_ENTRY entry)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Process ID: {entry.dwProcessID}");
            sb.AppendLine($"Executable Name: {entry.szName}");
            sb.AppendLine($"Flags: {entry.dwFlags:X}"); // Представление в шестнадцатеричном формате
            sb.AppendLine($"Start Time: {entry.dwTime0} ms");
            sb.AppendLine($"End Time: {entry.dwTime1} ms");
            sb.AppendLine($"Frames Rendered: {entry.dwFrames}");
            sb.AppendLine($"Frame Time: {entry.dwFrameTime} microseconds");
            sb.AppendLine($"Statistics Flags: {entry.dwStatFlags:X}");
            sb.AppendLine($"Statistics Start Time: {entry.dwStatTime0}");
            sb.AppendLine($"Statistics End Time: {entry.dwStatTime1}");
            sb.AppendLine($"Total Frames Rendered: {entry.dwStatFrames}");
            sb.AppendLine($"Statistics Count: {entry.dwStatCount}");
            sb.AppendLine($"Minimum Framerate: {entry.dwStatFramerateMin}");
            sb.AppendLine($"Average Framerate: {entry.dwStatFramerateAvg}");
            sb.AppendLine($"Maximum Framerate: {entry.dwStatFramerateMax}");
            sb.AppendLine($"OSD X-Coordinate: {entry.dwOSDX}");
            sb.AppendLine($"OSD Y-Coordinate: {entry.dwOSDY}");
            sb.AppendLine($"OSD Pixel Zoom: {entry.dwOSDPixel}");
            sb.AppendLine($"OSD Color: {entry.dwOSDColor:X}"); // В шестнадцатеричном представлении RGB
            sb.AppendLine($"OSD Frame ID: {entry.dwOSDFrame}");
            sb.AppendLine($"Screen Capture Flags: {entry.dwScreenCaptureFlags:X}");
            sb.AppendLine($"Screen Capture Path: {entry.szScreenCapturePath}");
            sb.AppendLine($"OSD Background Color: {entry.dwOSDBgndColor:X}");
            sb.AppendLine($"Video Capture Flags: {entry.dwVideoCaptureFlags:X}");
            sb.AppendLine($"Video Capture Path: {entry.szVideoCapturePath}");
            sb.AppendLine($"Video Framerate: {entry.dwVideoFramerate}");
            sb.AppendLine($"Video Frame Size: {entry.dwVideoFramesize}");
            sb.AppendLine($"Video Format: {entry.dwVideoFormat}");
            sb.AppendLine($"Video Quality: {entry.dwVideoQuality}");
            sb.AppendLine($"Video Capture Threads: {entry.dwVideoCaptureThreads}");
            sb.AppendLine($"Screen Capture Quality: {entry.dwScreenCaptureQuality}");
            sb.AppendLine($"Screen Capture Threads: {entry.dwScreenCaptureThreads}");
            sb.AppendLine($"Audio Capture Flags: {entry.dwAudioCaptureFlags:X}");
            sb.AppendLine($"Video Capture Flags Ex: {entry.dwVideoCaptureFlagsEx:X}");
            sb.AppendLine($"Audio Capture Flags 2: {entry.dwAudioCaptureFlags2:X}");
            sb.AppendLine($"Minimum Frame Time: {entry.dwStatFrameTimeMin}");
            sb.AppendLine($"Average Frame Time: {entry.dwStatFrameTimeAvg}");
            sb.AppendLine($"Maximum Frame Time: {entry.dwStatFrameTimeMax}");
            sb.AppendLine($"Frame Time Count: {entry.dwStatFrameTimeCount}");
            // Для буфера времени кадра и других подобных полей возможно потребуется специальная логика обработки

            return sb.ToString();
        }

    }
}
