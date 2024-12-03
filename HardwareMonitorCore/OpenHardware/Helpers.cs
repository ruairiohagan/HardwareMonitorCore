using System.Security.AccessControl;

namespace OpenHardwareMonitor.Hardware
{
    public static class FileExt
    {
        public static FileSecurity GetAccessControl(string fileName)
        {
            var fi = new FileInfo(fileName);
            var fsecurity = FileSystemAclExtensions.GetAccessControl(fi);

            return fsecurity;
        }

        public static void SetAccessControl(string fileName, FileSecurity fileSecurity)
        {
            var fi = new FileInfo(fileName);
            FileSystemAclExtensions.SetAccessControl(fi, fileSecurity);

        }
    }
}
