using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Checksums;
using System.Xml;

namespace GBSforDynamo
{
    class ZipUtil
    {
        public static Stream ZipStream(Stream inputStream, string fileName)
        {
            MemoryStream zipStream = new MemoryStream();
            ZipOutputStream outstream = new ZipOutputStream(zipStream);

            byte[] buffer = new byte[inputStream.Length];
            inputStream.Read(buffer, 0, buffer.Length);

            ZipEntry entry = new ZipEntry(fileName);
            outstream.PutNextEntry(entry);
            outstream.Write(buffer, 0, buffer.Length);
            outstream.Finish();
            zipStream.Position = 0;
            return zipStream;
        }

        //public static void ZipFile(string path, string file2Zip, string zipFileName, string zip, string bldgType)
            public static void ZipFile(string file2ZipFullPath, string zipFileFullPath)
        {
            //MemoryStream ms = InitializeGbxml(path + file2Zip, zip, bldgType) as MemoryStream;
            MemoryStream ms = InitializeGbxml(file2ZipFullPath) as MemoryStream;
 
            //string compressedFile = path + zipFileName;
            if (File.Exists(zipFileFullPath))
            {
                File.Delete(zipFileFullPath);
            }
            Crc32 objCrc32 = new Crc32();
            ZipOutputStream strmZipOutputStream = new ZipOutputStream(File.Create(zipFileFullPath));
            strmZipOutputStream.SetLevel(9);

            byte[] gbXmlBuffer = new byte[ms.Length];
            ms.Read(gbXmlBuffer, 0, gbXmlBuffer.Length);

            ZipEntry objZipEntry = new ZipEntry(file2ZipFullPath);

            objZipEntry.DateTime = DateTime.Now;
            objZipEntry.Size = ms.Length;
            ms.Close();
            objCrc32.Reset();
            objCrc32.Update(gbXmlBuffer);
            objZipEntry.Crc = objCrc32.Value;
            strmZipOutputStream.PutNextEntry(objZipEntry);
            strmZipOutputStream.Write(gbXmlBuffer, 0, gbXmlBuffer.Length);
            strmZipOutputStream.Finish();
            strmZipOutputStream.Close();
            strmZipOutputStream.Dispose();
        }

        public static string Unzip(string fileNameZip)
        {
            string path = Path.GetDirectoryName(fileNameZip);
            string folderName = Path.Combine(path, Path.GetFileNameWithoutExtension(fileNameZip));
            CreateDirectory(folderName);

            ZipInputStream strmZipInputStream = new ZipInputStream(File.OpenRead(fileNameZip));
            ZipEntry entry;
            while ((entry = strmZipInputStream.GetNextEntry()) != null)
            {
                FileStream fs = File.Create(Path.Combine(folderName, entry.Name));
                int size = 2048;
                byte[] data = new byte[2048];

                while (true)
                {
                    size = strmZipInputStream.Read(data, 0, data.Length);
                    if (size > 0)
                        fs.Write(data, 0, size);
                    else
                        break;
                }
                fs.Close();
            }
            strmZipInputStream.Close();
            return folderName;
        }

        //public static Stream InitializeGbxml(string fullname, string zipCode, string buildingType)
        public static Stream InitializeGbxml(string fullname)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(fullname);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("gbx", doc.DocumentElement.Attributes["xmlns"].Value);

            MemoryStream ms = new MemoryStream();
            doc.Save(ms);
            ms.Position = 0;

            return ms;
        }

        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

    }
}
