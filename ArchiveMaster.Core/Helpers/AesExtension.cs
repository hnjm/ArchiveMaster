﻿using FzLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArchiveMaster.Helpers;
using FzLib.IO;

namespace ArchiveMaster.Services
{
    public static class AesExtension
    {
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

        public static async Task DecryptFileAsync(this Aes manager, string sourcePath, string targetPath,
            int bufferLength = 0,
            IProgress<FileProcessProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (File.Exists(targetPath))
                throw new IOException($"目标文件{targetPath}已存在");

            if (bufferLength <= 0)
            {
                var fileInfo = new FileInfo(sourcePath);
                bufferLength = FileHelper.GetOptimalBufferSize(fileInfo.Length);
            }

            try
            {
                await using var streamSource = new FileStream(sourcePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, bufferLength, useAsync: true);
                await using var streamTarget = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, bufferLength, useAsync: true);

                byte[] iv = new byte[manager.BlockSize / 8];
                await streamSource.ReadAsync(iv, cancellationToken);
                manager.IV = iv;

                await using var cryptoStream = new CryptoStream(streamSource, manager.CreateDecryptor(),
                    CryptoStreamMode.Read, leaveOpen: false);

                byte[] buffer = new byte[bufferLength];
                long totalRead = 0;
                int read;
                long fileLength = streamSource.Length;
                long encryptedDataLength = fileLength - iv.Length;

                while ((read = await cryptoStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await streamTarget.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    totalRead += read;

                    progress?.Report(new FileProcessProgress
                    {
                        SourceFilePath = sourcePath,
                        DestinationFilePath = targetPath,
                        TotalBytes = encryptedDataLength,
                        ProcessedBytes = totalRead
                    });
                }

                try
                {
                    File.SetAttributes(targetPath, File.GetAttributes(sourcePath));
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                HandleException(targetPath, ex);
                throw;
            }
        }

        public static byte[] Encrypt(this Aes aes, byte[] plaintext, byte[] iv = null)
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

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] ciphertext = encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);

            byte[] result = new byte[iv.Length + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
            return result;
        }

        public static async Task EncryptFileAsync(this Aes manager, string sourcePath, string targetPath,
            int bufferLength = 0,
            IProgress<FileProcessProgress> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (File.Exists(targetPath))
                throw new IOException($"目标文件{targetPath}已存在");

            manager.GenerateIV();

            if (bufferLength <= 0)
            {
                var fileInfo = new FileInfo(sourcePath);
                bufferLength = FileHelper.GetOptimalBufferSize(fileInfo.Length);
            }

            try
            {
                await using var streamSource = new FileStream(sourcePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, bufferLength, useAsync: true);
                await using var streamTarget = new FileStream(targetPath, FileMode.CreateNew, FileAccess.Write,
                    FileShare.None, bufferLength, useAsync: true);

                await streamTarget.WriteAsync(manager.IV, 0, manager.IV.Length, cancellationToken);

                await using var cryptoStream = new CryptoStream(streamTarget, manager.CreateEncryptor(),
                    CryptoStreamMode.Write, leaveOpen: false);

                byte[] buffer = new byte[bufferLength];
                long totalRead = 0;
                int read;
                long fileLength = streamSource.Length;

                while ((read = await streamSource.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await cryptoStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                    totalRead += read;

                    progress?.Report(new FileProcessProgress
                    {
                        SourceFilePath = sourcePath,
                        DestinationFilePath = targetPath,
                        TotalBytes = fileLength,
                        ProcessedBytes = totalRead
                    });
                }

                await cryptoStream.FlushAsync(cancellationToken);
                await cryptoStream.FlushFinalBlockAsync(cancellationToken);

                try
                {
                    File.SetAttributes(targetPath, File.GetAttributes(sourcePath));
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                HandleException(targetPath, ex);
                throw;
            }
        }

        public static Aes GetDefault(string password)
        {
            Aes aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.SetStringKey(password);
            return aes;
        }

        public static long GetEncryptedFileSize(long originalSize, int blockSizeBytes = 16, int ivSizeBytes = 16,
            PaddingMode padding = PaddingMode.PKCS7)
        {
            long paddedLength = padding switch
            {
                PaddingMode.None => originalSize,
                PaddingMode.Zeros => (originalSize % blockSizeBytes == 0)
                    ? originalSize
                    : ((originalSize / blockSizeBytes) + 1) * blockSizeBytes,
                PaddingMode.PKCS7 => ((originalSize / blockSizeBytes) + 1) * blockSizeBytes,
                PaddingMode.ANSIX923 => ((originalSize / blockSizeBytes) + 1) * blockSizeBytes,
                PaddingMode.ISO10126 => ((originalSize / blockSizeBytes) + 1) * blockSizeBytes,
                _ => throw new NotSupportedException($"不支持的填充模式：{padding}")
            };

            return ivSizeBytes + paddedLength;
        }

        public static long GetEncryptedFileSize(this Aes aes, long originalSize)
        {
            return GetEncryptedFileSize(originalSize, aes.BlockSize / 8, aes.BlockSize / 8, aes.Padding);
        }


        public static Aes SetStringKey(this Aes manager, string key)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes(nameof(ArchiveMaster)), 100000,
                HashAlgorithmName.SHA256);
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