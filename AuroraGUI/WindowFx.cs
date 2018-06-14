using System;
using System.Runtime.InteropServices;

namespace AuroraGUI
{
    class WindowFx
    {
            [DllImport("user32.dll", EntryPoint = "AnimateWindow")]
            public static extern bool AnimateWindows(IntPtr handle, int effectsTime, int effectsFlags);
            public const Int32 AW_HOR_POSITIVE = 0x00000001;
            public const Int32 AW_HOR_NEGATIVE = 0x00000002;
            public const Int32 AW_VER_POSITIVE = 0x00000004;
            public const Int32 AW_VER_NEGATIVE = 0x00000008;
            public const Int32 AW_CENTER = 0x00000010;
            public const Int32 AW_HIDE = 0x00010000;
            public const Int32 AW_ACTIVATE = 0x00020000;
            public const Int32 AW_SLIDE = 0x00040000;
            public const Int32 AW_BLEND = 0x00080000;
    }
}
