using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace BizHawk.Client.Common
{
	public sealed class MemoryMappedFiles
	{
		private readonly Dictionary<string, MemoryMappedFile> _mmfFiles = new Dictionary<string, MemoryMappedFile>();

		private readonly Dictionary<int, MemoryMappedFile> _mmfFilesOnDisk = new Dictionary<int, MemoryMappedFile>();

		private readonly Func<byte[]> _takeScreenshotCallback;

		public string Filename;

		public MemoryMappedFiles(Func<byte[]> takeScreenshotCallback, string filename)
		{
			_takeScreenshotCallback = takeScreenshotCallback;
			Filename = filename;
		}

		public string ReadFromFile(string filename, int expectedSize)
		{
			var bytes = ReadBytesFromFile(filename, expectedSize);
			return Encoding.UTF8.GetString(bytes);
		}

		public byte[] ReadBytesFromFile(string filename, int expectedSize)
		{
			if (!_mmfFiles.TryGetValue(filename, out var mmfFile))
			{
				mmfFile = _mmfFiles[filename] = MemoryMappedFile.OpenExisting(filename);
			}

			using var viewAccessor = mmfFile.CreateViewAccessor(0, expectedSize, MemoryMappedFileAccess.Read);
			var bytes = new byte[expectedSize];
			viewAccessor.ReadArray(0, bytes, 0, expectedSize);
			return bytes;
		}

		public int ScreenShotToFile()
		{
			if (Filename is null)
			{
				Console.WriteLine("MMF screenshot target not set; start EmuHawk with `--mmf=filename`");
				return 0;
			}
			return WriteToFile(Filename, _takeScreenshotCallback());
		}

		public (MemoryMappedFile MappedFile, int Handle) OpenOrCreateFile(string filePath, long size)
		{
			var fileName = Path.GetFileName(filePath);
			var file = File.Open(
				path: filePath,
				mode: FileMode.OpenOrCreate,
				access: FileAccess.ReadWrite,
				share: FileShare.ReadWrite
			);
			var mmf = MemoryMappedFile.CreateFromFile(
				fileStream: file,
				mapName: fileName,
				capacity: size,
				access: MemoryMappedFileAccess.ReadWrite,
				inheritability: HandleInheritability.Inheritable,
				leaveOpen: false
			);
			var handle = mmf.SafeMemoryMappedFileHandle.DangerousGetHandle()
				.ToInt32();
			_mmfFilesOnDisk[handle] = mmf;
			return (MappedFile: mmf, Handle: handle);
		}

		public int WriteToMappedFile(int handle, byte[] outputBytes)
		{
			if (!_mmfFilesOnDisk.TryGetValue(handle, out var mmf))
			{
				throw new IOException($"No cached file found for given handle '{handle}'");
			}

			if (mmf.SafeMemoryMappedFileHandle.IsClosed)
			{
				throw new IOException($"Mapped file handle '{handle}' is closed");
			}
			
			if (mmf.SafeMemoryMappedFileHandle.IsInvalid)
			{
				throw new IOException($"Mapped file handle '{handle}' is invalid");
			}
			
			using var accessor = mmf.CreateViewAccessor(0, outputBytes.LongLength, MemoryMappedFileAccess.ReadWrite);
			accessor.WriteArray(0, outputBytes, 0, outputBytes.Length);
			return outputBytes.Length;
		}

		public bool DisposeMappedFile(int handle)
		{
			if (!_mmfFilesOnDisk.TryGetValue(handle, out var mmf))
			{
				return false;
			}

			_mmfFilesOnDisk.Remove(handle);

			if (mmf.SafeMemoryMappedFileHandle.IsClosed)
			{
				return false;
			}
			
			if (mmf.SafeMemoryMappedFileHandle.IsInvalid)
			{
				return false;
			}
			
			mmf.Dispose();
			return true;
		}

		public int WriteToFile(string filename, byte[] outputBytes)
		{
			int TryWrite(MemoryMappedFile m)
			{
				using var accessor = m.CreateViewAccessor(0, outputBytes.Length, MemoryMappedFileAccess.Write);
				accessor.WriteArray(0, outputBytes, 0, outputBytes.Length);
				return outputBytes.Length;
			}

			if (!_mmfFiles.TryGetValue(filename, out var mmfFile))
			{
				mmfFile = _mmfFiles[filename] = MemoryMappedFile.CreateOrOpen(filename, outputBytes.Length);
			}

			try
			{
				return TryWrite(mmfFile);
			}
			catch (UnauthorizedAccessException)
			{
				try
				{
					mmfFile.Dispose();
				}
				catch (Exception)
				{
					// ignored
					//TODO are Dispose() implementations allowed to throw? does this one ever throw? --yoshi
				}
				return TryWrite(_mmfFiles[filename] = MemoryMappedFile.CreateOrOpen(filename, outputBytes.Length));
			}
		}

		public int WriteToFile(string filename, string outputString) => WriteToFile(filename, Encoding.UTF8.GetBytes(outputString));
	}
}
