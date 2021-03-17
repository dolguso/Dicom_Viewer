using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

#region
/*
 * System.Runtime.InteropServices supports Interops between managed code and unmanaged code
 */
using System.Runtime.InteropServices;
#endregion

namespace JSS_01K.ClassFiles
{
    static class S_DllWrapper
    {
        [DllImport("jssv26.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr GetYOLOv2(string device, string xmlPath, string binPath);

        [DllImport("jssv26.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern IntPtr Init(IntPtr YOLOv2);

        [DllImport("jssv26.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern IntPtr Infer(IntPtr YOLOv2, IntPtr MatPtr, ref IntPtr buffer, ref int length);
    }

    static class P_DllWrapper
    {
        [DllImport("jpsv26.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr GetYOLOv2(string device, string xmlPath, string binPath);
    
        [DllImport("jpsv26.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern IntPtr Init(IntPtr YOLOv2);

        [DllImport("jpsv26.dll", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern IntPtr Infer(IntPtr YOLOv2, IntPtr MatPtr, ref IntPtr buffer, ref int length);
    }

    public struct DetectionObject
    {
        public int xmin, ymin, xmax, ymax, class_id;
        public float confidence;
    }
}
