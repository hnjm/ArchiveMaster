using FzLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArchiveMaster.Helpers;

namespace ArchiveMaster.Services
{
    public static class AesExtension
    {
        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="array">要解密的 byte[] 数组</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] Decrypt(this Aes aes, byte[] encryptedDataWithIv)
        {
            int ivSize = aes.BlockSize / 8; // AES IV 固定为 16 字节
            if (encryptedDataWithIv.Length < ivSize)
            {
                throw new ArgumentException("无效的加密数据：缺少 IV");
            }

            byte[] iv = new byte[ivSize];
            byte[] ciphertext = new byte[encryptedDataWithIv.Length - ivSize];
    
            Buffer.BlockCopy(encryptedDataWithIv, 0, iv, 0, ivSize);
            Buffer.BlockCopy(encryptedDataWithIv, ivSize, ciphertext, 0, ciphertext.Length);

            aes.IV = iv;
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        public static void DecryptFile(this Aes manager, string sourcePath, string targetPath,
            int bufferLength = 0,
            IProgress<FileCopyProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (File.Exists(targetPath))
            {
                throw new IOException($"目标文件{targetPath}已存在");
            }

            if (bufferLength <= 0)
            {
                var fileInfo = new FileInfo(sourcePath);
                bufferLength = FileIOHelper.GetOptimalBufferSize(fileInfo.Length);
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

                    progress?.Report(new FileCopyProgress
                    {
                        SourceFilePath = sourcePath,
                        DestinationFilePath = targetPath,
                        TotalBytes = fileLength,
                        BytesCopied = currentSize
                    });
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

                        progress?.Report(new FileCopyProgress
                        {
                            SourceFilePath = sourcePath,
                            DestinationFilePath = targetPath,
                            TotalBytes = fileLength,
                            BytesCopied = currentSize
                        });
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

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="array">要加密的 byte[] 数组</param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static byte[] Encrypt(this Aes aes, byte[] plaintext,byte[] iv=null)
        {
            if (iv == null)
            {
                aes.GenerateIV();
                iv = aes.IV;
            }
            else if (iv.Length != 16)
            {
                throw new Exception("iv应当为空表示自动生成，或提供一个长度为16的字符数组");
            }

            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            {
                byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        
                byte[] result = new byte[iv.Length + ciphertext.Length];
                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
                return result;
            }
        }
        public static void EncryptFile(this Aes manager, string sourcePath, string targetPath,
            int bufferLength = 0,
            IProgress<FileCopyProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (File.Exists(targetPath))
            {
                throw new IOException($"目标文件{targetPath}已存在");
            }
            manager.GenerateIV();
            if (bufferLength <= 0)
            {
                var fileInfo = new FileInfo(sourcePath);
                bufferLength = FileIOHelper.GetOptimalBufferSize(fileInfo.Length);
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

                        progress?.Report(new FileCopyProgress
                        {
                            SourceFilePath = sourcePath,
                            DestinationFilePath = targetPath,
                            TotalBytes = fileLength,
                            BytesCopied = currentSize
                        });
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

        public static Aes SetStringKey(this Aes manager, string key)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes(nameof(ArchiveMaster)), 100000,HashAlgorithmName.SHA256);
            manager.Key = deriveBytes.GetBytes(manager.KeySize / 8);
            return manager;
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
    }
}