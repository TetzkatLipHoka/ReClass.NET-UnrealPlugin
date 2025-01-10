using ReClassNET.Extensions;
using ReClassNET.Memory;
using ReClassNET.MemoryScanner;
using ReClassNET.Nodes;
using ReClassNET.Plugins;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnrealPlugin
{
	public class UnrealPluginExt : Plugin
	{
		private IPluginHost host;
		public static IntPtr gNames;
		public static IntPtr gObjects;

		public override bool Initialize(IPluginHost host)
		{
			this.host = host;
			this.host.Process.ProcessAttached += OnProcessAttached;

			return true;
		}

		public override void Terminate()
		{
			this.host.Process.ProcessAttached -= OnProcessAttached;
			host = null;
		}

		public override IReadOnlyList<INodeInfoReader> GetNodeInfoReaders()
		{
			return new[] { new UnrealNodeInfoReader() };
		}

        private static IntPtr FindPattern(RemoteProcess process, Module module, string pattern)
        {
            var moduleBytes = process.ReadRemoteMemory(module.Start, module.Size.ToInt32());
            var bytePattern = BytePattern.Parse(pattern);

            var limit = moduleBytes.Length - bytePattern.Length;
            for (var i = 0; i < limit; ++i)
                if (bytePattern.Equals(moduleBytes, i))
                    return module.Start + i;

            return IntPtr.Zero;
        }

        private static IntPtr FindPatternByModuleName(RemoteProcess process, string processName, string pattern)
        {
            var module = process.GetModuleByName(processName);
            return FindPattern(process, module, pattern);
        }

        //Make this cleaner like jesus this is terrible
        // Define named constants for offsets
        const int
			Offset1 = 0x3,
			Offset2 = 0x7;

		// Helper function to find a pattern and calculate an address
		private IntPtr FindAndReadAddress(RemoteProcess process, string processName, string pattern)
		{
			var address = FindPatternByModuleName(process, processName, pattern);

			if (!address.IsNull())
			{
				var offset = process.ReadRemoteInt32(address + Offset1);
				return process.ReadRemoteIntPtr(address + offset + Offset2);
			}

			return IntPtr.Zero;
		}

		private void OnProcessAttached(RemoteProcess process)
		{
			process.UpdateProcessInformations();

			var processName = System.IO.Path.GetFileName(process.UnderlayingProcess.Path).ToLower();

			//Add new shit
			//Make definitions to switch the patterns
			string
				gNamesPattern,
				gObjectPattern;

			// Usage in your main code
			switch (processName)
			{
				case "atomicheart-win64-shipping.exe": // AtomicHeart
                {
					//48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B C0 C6 05 ?? ?? ?? ?? ?? 48 8B 45 ?? 48 8D
					//48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 4C 8B C0 C6 05 ?? ?? ?? ?? ?? 48 8B 45 ?? 48 8D 55 ?? 48 C1 E8 ?? 8D 0C 00 49 03 4C D8 ?? E8 ?? ?? ?? ?? 4C 8B 7D ?? 4C 89 7D ?? EB ?? 48 8D 55 ?? 48 89 7D ?? 48 8D 4D ?? 48 89 7D ?? E8 ?? ?? ?? ?? 48 8B 45 ?? 48 89 45 ?? 44 8B 6D ?? 48 89 7D ?? 48 C7 45 ?? ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8D 4D ?? 48 8B F8 E8 ?? ?? ?? ?? 48 8B 5D ?? 48 8D 57 ?? 48 8B CB E8
					//0x729ABC0
					gNamesPattern = "4C 8D 05 ?? ?? ?? ?? 48 8B 55 ?? 3B C1 75 ?? 48 8B 4D ?? 48 39 4D ?? 75 ?? 48 3B D6 0F 84 ?? ?? ?? ?? 48 8D 0C 40 48 8B 02 0F 10 04 C8 0F 11 44 24 ?? 48 8B 44 24 ?? 48 8B C8 0F B7 F8 8B D8 48 C1 E9 ?? C1 EB ?? 48 3B 45";
					gNames = FindAndReadAddress(process, processName, gNamesPattern);


					//Jumps to UObject::ProcessEvent
					//0x27F1C56 48 8B 05 73 2E 88 04 mov rax, cs:FUObjectArray
					gObjectPattern = "48 8B 05 ?? ?? ?? ?? 48 8B 0C C8 48 8D 04 D1 EB ?? 49 8B C6 8B 40 ?? C1 E8 ?? A8 ?? 0F 85 ?? ?? ?? ?? F7 86 ?? ?? ?? ?? ?? ?? ?? ?? 74 ?? 49 8B 07 45 33 C0 48 8B D6 49 8B CF FF 90 ?? ?? ?? ?? 8B D8 A8 ?? 74 ?? 4D 8B";
					gObjects = FindAndReadAddress(process, processName, gObjectPattern);
					break;
				}

				case "Dishonored.exe": // Dishonored
				{
					gNamesPattern = "8B 0D ?? ?? ?? ?? 83 3C 81 00 74";
					gNames = FindPatternByModuleName(process, processName, gNamesPattern);
					if (gNames != null)
						gNames = gNames + 0x2;

					gObjectPattern = "A1 ?? ?? ?? ?? 8B ?? ?? 8B ?? ?? 25 00 02 00 00";
					gObjects = FindPatternByModuleName(process, processName, gObjectPattern);
					if (gObjects != null)
						gObjects = gObjects + 0x1;

					break;
				}

				case "fortniteclient-win64-shipping.exe": // Fortnite
				{
					gNamesPattern = "48 89 1D ?? ?? ?? ?? 48 8B 5C 24 ?? 48 83 C4 28 C3 48 8B 5C 24 ?? 48 89 05 ?? ?? ?? ?? 48 83 C4 28 C3";
					gNames = FindAndReadAddress(process, processName, gNamesPattern);

					gObjectPattern = "48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B D6 48 89 B5";
					gObjects = FindAndReadAddress(process, processName, gObjectPattern);
					break;
				}

				case "sotgame.exe": // Sea of Thieves
				{
					gNamesPattern = "48 8B 1D ?? ?? ?? ?? 48 85 ?? 75 3A";
					gNames = FindAndReadAddress(process, processName, gNamesPattern);

					gObjectPattern = "48 8B 15 ?? ?? ?? ?? 3B 42 1C";
					gObjects = FindAndReadAddress(process, processName, gObjectPattern);

					break;
				}

				case "tslgame.exe": // Playerunknown's Battlegrounds
					break;
			}
		}
	}

	public class UnrealNodeInfoReader : INodeInfoReader
	{
		public string ReadNodeInfo(BaseHexCommentNode node, IRemoteMemoryReader reader, MemoryBuffer memory, IntPtr nodeAddress, IntPtr nodeValue)
		{
			if (IsUObject(nodeValue, reader))
			{
				return GetUObjectName(nodeValue, reader);
			}

			return null;
		}

		private bool IsUObject(IntPtr objectPtr, IRemoteMemoryReader reader)
		{
			if (UnrealPluginExt.gObjects.IsNull())
				return false;

			var internalIndex = reader.ReadRemoteInt32(objectPtr + IntPtr.Size + 0x4);

			var numElements = reader.ReadRemoteInt32(UnrealPluginExt.gObjects + 0x10 + IntPtr.Size + 0x4);

			if (internalIndex < 0 || internalIndex >= numElements) return false;

			var objects = reader.ReadRemoteIntPtr(UnrealPluginExt.gObjects + 0x10);

#if RECLASSNET64
			var objectPtrCheck = reader.ReadRemoteIntPtr(objects + internalIndex * 0x18);
#else
			var objectPtrCheck = reader.ReadRemoteIntPtr(objects + internalIndex * 0x10);
#endif

			bool result = objectPtr == objectPtrCheck;

			return result;
		}

		private string GetUObjectName(IntPtr objectPtr, IRemoteMemoryReader reader)
		{
			if (UnrealPluginExt.gNames.IsNull())
			{
				return null;
			}

			var classObject = reader.ReadRemoteIntPtr(objectPtr + 0x10);

#if RECLASSNET64
			var nameIndex = reader.ReadRemoteInt32(classObject + 0x18);
#else
			var nameIndex = reader.ReadRemoteInt32(objectPtr + 0x10);
#endif

			if (nameIndex < 1)
			{
				return null;
			}

			var numElements = reader.ReadRemoteInt32(UnrealPluginExt.gNames + 0x80 * IntPtr.Size);
			var numChunks = reader.ReadRemoteInt32(UnrealPluginExt.gNames + 0x80 * IntPtr.Size + 0x4);

			var indexChunk = nameIndex / 16384;
			var indexName = nameIndex % 16384;

			if (nameIndex < numElements && indexChunk < numChunks)
			{
				var chunkPtr = reader.ReadRemoteIntPtr(UnrealPluginExt.gNames + indexChunk * IntPtr.Size);

				if (chunkPtr.MayBeValid())
				{
					var namePtr = reader.ReadRemoteIntPtr(chunkPtr + indexName * IntPtr.Size);

					var nameEntryIndex = reader.ReadRemoteInt32(namePtr);

					if (nameEntryIndex >> 1 == nameIndex)
					{
						var wideChar = (nameEntryIndex & 1) != 0;

						// Calculate the correct memory address of the string
						IntPtr stringAddress = namePtr + 0x8 + IntPtr.Size;

						// Now, call the ReadRemoteString method with the correct arguments
						var name = reader.ReadRemoteString(stringAddress, wideChar ? Encoding.Unicode : Encoding.ASCII, 1024);


						//var name = reader.ReadRemoteString(wideChar ? Encoding.Unicode : Encoding.ASCII, namePtr + 0x8 + IntPtr.Size, 1024);

						return name;
					}
				}
			}

			return null;
		}
	}
}
