using FzLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchiveMaster.Services
{
    public static class AesExtension
    {
        public static Aes SetStringKey(this Aes manager, string key, char fill = (char)0, Encoding encoding = null)
        {
            manager.Key = GetBytesFromString(manager, key, fill, encoding);
            return manager;
        }

        public static Aes SetStringIV(this Aes manager, string iv, char fill = (char)0, Encoding encoding = null)
        {
            manager.IV = GetBytesFromString(manager, iv, fill, encoding);
            return manager;
        }

        private static byte[] GetBytesFromString(Aes manager, string input, char fill, Encoding encoding)
        {
            input ??= "";
            int length = manager.BlockSize / 8;
            if (input.Length < length)
            {
                input += new string(fill, length - input.Length);
            }
            else if (input.Length > length)
            {
                input = input[..length];
            }

            return (encoding ?? Encoding.UTF8).GetBytes(input);
        }


        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="array">要加密的 byte[] 数组</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] Encrypt(this Aes manager, byte[] array)
        {
            var encryptor = manager.CreateEncryptor();
            return encryptor.TransformFinalBlock(array, 0, array.Length);
        }

        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="array">要解密的 byte[] 数组</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] Decrypt(this Aes manager, byte[] array)
        {
            var decryptor = manager.CreateDecryptor();
            return decryptor.TransformFinalBlock(array, 0, array.Length);
        }

        public static void EncryptStreamToStream(this Aes manager, Stream streamInput, Stream streamOutput,
            int bufferLength = 1024 * 1024)
        {
            if (!streamInput.CanRead)
            {
                throw new Exception("输入流不可读");
            }

            if (!streamOutput.CanWrite)
            {
                throw new Exception("输出流不可写");
            }

            using var encryptor = manager.CreateEncryptor();
            byte[] input = new byte[bufferLength];
            byte[] output = new byte[bufferLength];
            int size;
            long length = streamInput.Length;
            while ((size = streamInput.Read(input, 0, bufferLength)) != 0)
            {
                if (streamInput.Position == length)
                {
                    output = encryptor.TransformFinalBlock(input, 0, size);
                }
                else
                {
                    encryptor.TransformBlock(input, 0, size, output, 0);
                }

                streamOutput.Write(output, 0, output.Length);
                streamOutput.Flush();
            }
        }

        public static void DecryptStreamToStream(this Aes manager, Stream streamInput, Stream streamOutput,
            int bufferLength = 1024 * 1024)
        {
            if (!streamInput.CanRead)
            {
                throw new Exception("输入流不可读");
            }

            if (!streamOutput.CanWrite)
            {
                throw new Exception("输出流不可写");
            }

            using var encryptor = manager.CreateDecryptor();
            byte[] input = new byte[bufferLength];
            byte[] output = new byte[bufferLength];
            int size;
            int outputSize = 0;
            long length = streamInput.Length;
            while ((size = streamInput.Read(input, 0, bufferLength)) != 0)
            {
                if (streamInput.Position == length)
                {
                    output = encryptor.TransformFinalBlock(input, 0, size);
                    outputSize = output.Length;
                }
                else
                {
                    outputSize = encryptor.TransformBlock(input, 0, size, output, 0);
                }

                streamOutput.Write(output, 0, outputSize);
                streamOutput.Flush();
            }
        }

        public static void EncryptFile(this Aes manager, string sourcePath, string targetPath,
            CancellationToken cancellationToken,
            int bufferLength = 1024 * 1024,
            RefreshFileProgress refreshFileProgress = null)
        {
            if (File.Exists(targetPath))
            {
                throw new IOException($"目标文件{targetPath}已存在");
            }

            try
            {
                using (FileStream streamSource = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
                using (FileStream streamTarget = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    streamTarget.Write(manager.IV, 0, manager.IV.Length);
                    using var encryptor = manager.CreateEncryptor();
                    long currentSize = 0;
                    int size;
                    byte[] input = new byte[bufferLength];
                    long fileLength = streamSource.Length;

                    while ((size = streamSource.Read(input, 0, bufferLength)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        byte[] output;

                        if (streamSource.Position == fileLength)
                        {
                            output = encryptor.TransformFinalBlock(input, 0, size);
                        }
                        else
                        {
                            output = new byte[size];
                            encryptor.TransformBlock(input, 0, size, output, 0);
                        }

                        currentSize += output.Length;
                        streamTarget.Write(output, 0, output.Length);
                        streamTarget.Flush();
                        refreshFileProgress?.Invoke(sourcePath, targetPath, fileLength, currentSize); //更新进度
                    }
                }

                new FileInfo(targetPath).Attributes = File.GetAttributes(sourcePath);
            }
            catch (Exception ex)
            {
                HandleException(targetPath, ex);
                throw;
            }
        }

        public static void DecryptFile(this Aes manager, string sourcePath, string targetPath,
            CancellationToken cancellationToken,
            int bufferLength = 1024 * 1024,
            RefreshFileProgress refreshFileProgress = null)
        {
            if (File.Exists(targetPath))
            {
                throw new IOException($"目标文件{targetPath}已存在");
            }

            try
            {
                using (FileStream streamSource = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
                using (FileStream streamTarget = new FileStream(targetPath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    byte[] iv = new byte[16];
                    streamSource.Read(iv, 0, iv.Length);
                    manager.IV = iv;
                    using var decryptor = manager.CreateDecryptor();
                    long currentSize = 0;
                    int size;
                    byte[] input = new byte[bufferLength];
                    long fileLength = streamSource.Length;
                    refreshFileProgress?.Invoke(sourcePath, targetPath, fileLength, currentSize); //更新进度

                    while ((size = streamSource.Read(input, 0, bufferLength)) > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        byte[] output;
                        int outputSize;

                        if (streamSource.Position == fileLength)
                        {
                            output = decryptor.TransformFinalBlock(input, 0, size);
                            outputSize = output.Length;
                        }
                        else
                        {
                            output = new byte[size];
                            outputSize = decryptor.TransformBlock(input, 0, size, output, 0);
                        }

                        currentSize += outputSize;
                        streamTarget.Write(output, 0, outputSize);
                        streamTarget.Flush();
                        refreshFileProgress?.Invoke(sourcePath, targetPath, fileLength, currentSize); //更新进度
                    }
                }

                new FileInfo(targetPath).Attributes = File.GetAttributes(sourcePath);
            }
            catch (Exception ex)
            {
                HandleException(targetPath, ex);
                throw;
            }
        }

        private static void HandleException(string target, Exception ex)
        {
            try
            {
                File.Delete(target);
            }
            catch
            {
            }

            throw ex;
        }

        /// <summary>
        /// 更新文件加密进度
        /// </summary>
        public delegate void RefreshFileProgress(string source, string target, long max, long value);
    }
}