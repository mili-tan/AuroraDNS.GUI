using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Interop;

// ReSharper disable UnusedMember.Global
// ReSharper disable SuspiciousTypeConversion.Global

namespace Lnk
{
    internal static class NativeClasses
    {
        [Flags]
        internal enum SLR_MODE : uint
        {
            SLR_INVOKE_MSI = 0x80,
            SLR_NOLINKINFO = 0x40,
            SLR_NO_UI = 0x1,
            SLR_NOUPDATE = 0x8,
            SLR_NOSEARCH = 0x10,
            SLR_NOTRACK = 0x20,
            SLR_UPDATE = 0x4,
            SLR_NO_UI_WITH_MSG_PUMP = 0x101
        }

        [Flags]
        internal enum STGM_ACCESS : uint
        {
            STGM_READ = 0x00000000,
            STGM_WRITE = 0x00000001,
            STGM_READWRITE = 0x00000002,
            STGM_SHARE_DENY_NONE = 0x00000040,
            STGM_SHARE_DENY_READ = 0x00000030,
            STGM_SHARE_DENY_WRITE = 0x00000020,
            STGM_SHARE_EXCLUSIVE = 0x00000010,
            STGM_PRIORITY = 0x00040000,
            STGM_CREATE = 0x00001000,
            STGM_CONVERT = 0x00020000,
            STGM_FAILIFTHERE = 0x00000000,
            STGM_DIRECT = 0x00000000,
            STGM_TRANSACTED = 0x00010000,
            STGM_NOSCRATCH = 0x00100000,
            STGM_NOSNAPSHOT = 0x00200000,
            STGM_SIMPLE = 0x08000000,
            STGM_DIRECT_SWMR = 0x00400000,
            STGM_DELETEONRELEASE = 0x04000000
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0)]
        internal struct _FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Unicode)]
        internal struct _WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public _FILETIME ftCreationTime;
            public _FILETIME ftLastAccessTime;
            public _FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        internal const uint SLGP_SHORTPATH = 0x01;
        internal const uint SLGP_UNCPRIORITY = 0x02;
        internal const uint SLGP_RAWPATH = 0x04;

        [ComImport()]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellLinkW
        {
            [PreserveSig()]
            int GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, ref _WIN32_FIND_DATAW pfd, uint fFlags);

            [PreserveSig()]
            int GetIDList(out IntPtr ppidl);

            [PreserveSig()]
            int SetIDList(IntPtr pidl);

            [PreserveSig()]
            int GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxName);

            [PreserveSig()]
            int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

            [PreserveSig()]
            int GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

            [PreserveSig()]
            int SetWorkingDirectory(
               [MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            [PreserveSig()]
            int GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

            [PreserveSig()]
            int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

            [PreserveSig()]
            int GetHotkey(out ushort pwHotkey);

            [PreserveSig()]
            int SetHotkey(ushort pwHotkey);

            [PreserveSig()]
            int GetShowCmd(out uint piShowCmd);

            [PreserveSig()]
            int SetShowCmd(uint piShowCmd);

            [PreserveSig()]
            int GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

            [PreserveSig()]
            int SetIconLocation(
               [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

            [PreserveSig()]
            int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

            [PreserveSig()]
            int Resolve(IntPtr hWnd, uint fFlags);

            [PreserveSig()]
            int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport()]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPersistFile
        {
            [PreserveSig()]
            int GetClassID(out Guid pClassID);

            [PreserveSig()]
            int IsDirty();

            [PreserveSig()]
            int Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

            [PreserveSig()]
            int Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

            [PreserveSig()]
            int SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

            [PreserveSig()]
            int GetCurFile([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath);
        }

        [Guid("00021401-0000-0000-C000-000000000046")]
        [ClassInterface(ClassInterfaceType.None)]
        [ComImport()]
        private class CShellLink { }

        internal static IShellLinkW CreateShellLink()
        {
            return new CShellLink() as IShellLinkW;
        }
    }

    public class Shortcut
    {
        private const int MAX_DESCRIPTION_LENGTH = 512;
        private const int MAX_PATH = 512;

        private NativeClasses.IShellLinkW _link;

        public Shortcut()
        {
            _link = NativeClasses.CreateShellLink();
        }

        public Shortcut(string path)
            : this()
        {
            Marshal.ThrowExceptionForHR(_link.SetPath(path));
        }

        public string Path
        {
            get
            {
                NativeClasses._WIN32_FIND_DATAW fdata = new NativeClasses._WIN32_FIND_DATAW();
                StringBuilder path = new StringBuilder(MAX_PATH, MAX_PATH);
                Marshal.ThrowExceptionForHR(_link.GetPath(path, path.MaxCapacity, ref fdata, NativeClasses.SLGP_UNCPRIORITY));
                return path.ToString();
            }
            set => Marshal.ThrowExceptionForHR(_link.SetPath(value));
        }

        public string Description
        {
            get
            {
                StringBuilder desc = new StringBuilder(MAX_DESCRIPTION_LENGTH, MAX_DESCRIPTION_LENGTH);
                Marshal.ThrowExceptionForHR(_link.GetDescription(desc, desc.MaxCapacity));
                return desc.ToString();
            }
            set => Marshal.ThrowExceptionForHR(_link.SetDescription(value));
        }

        public string RelativePath
        {
            set => Marshal.ThrowExceptionForHR(_link.SetRelativePath(value, 0));
        }

        public string WorkingDirectory
        {
            get
            {
                StringBuilder dir = new StringBuilder(MAX_PATH, MAX_PATH);
                Marshal.ThrowExceptionForHR(_link.GetWorkingDirectory(dir, dir.MaxCapacity));
                return dir.ToString();
            }
            set => Marshal.ThrowExceptionForHR(_link.SetWorkingDirectory(value));
        }

        public string Arguments
        {
            get
            {
                StringBuilder args = new StringBuilder(MAX_PATH, MAX_PATH);
                Marshal.ThrowExceptionForHR(_link.GetArguments(args, args.MaxCapacity));
                return args.ToString();
            }
            set => Marshal.ThrowExceptionForHR(_link.SetArguments(value));
        }

        public ushort HotKey
        {
            get
            {
                Marshal.ThrowExceptionForHR(_link.GetHotkey(out var key));
                return key;
            }
            set => Marshal.ThrowExceptionForHR(_link.SetHotkey(value));
        }

        public void Resolve(IntPtr hwnd, uint flags)
        {
            Marshal.ThrowExceptionForHR(_link.Resolve(hwnd, flags));
        }

        public void Resolve(IWin32Window window)
        {
            Resolve(window.Handle, 0);
        }

        public void Resolve()
        {
            Resolve(IntPtr.Zero, (uint)NativeClasses.SLR_MODE.SLR_NO_UI);
        }

        private NativeClasses.IPersistFile AsPersist => (_link as NativeClasses.IPersistFile);

        public void Save(string fileName)
        {
            int hres = AsPersist.Save(fileName, true);
            Marshal.ThrowExceptionForHR(hres);
        }

        public void Load(string fileName)
        {
            int hres = AsPersist.Load(fileName, (uint)NativeClasses.STGM_ACCESS.STGM_READ);
            Marshal.ThrowExceptionForHR(hres);
        }
    }
}