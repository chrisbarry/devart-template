//
// Native.cs
//
// Authors:
//    Aaron Bockover  <abockover@novell.com>
//
// Copyright (C) 2006-2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;
using System.Threading;

namespace Mono.Zeroconf.Providers.Bonjour
{
	[AttributeUsage (AttributeTargets.Method)]
	public sealed class MonoPInvokeCallbackAttribute : Attribute {
		public MonoPInvokeCallbackAttribute (Type t) {}
	}

	public enum SockAddrFamily
	{
		Inet = 2,
		Inet6 = 23,
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SockAddr
	{
		public ushort Family;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
		public byte[] Data;
	};
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SockAddrIn 
	{
		public ushort Family;
		public ushort Port;
		public uint Addr;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public byte[] Zero;
	}
	
	[StructLayout(LayoutKind.Sequential)]
	public struct SockAddrIn6 
	{
		public ushort Family;
		public ushort Port;
		public uint FlowInfo;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] 
		public byte[] Addr;
		public uint ScopeId;
	};
	

	//	[StructLayout(LayoutKind.Sequential)]
//	public struct SockAddr
//	{
//		public ushort Family;
//		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
//		public byte[] Data;
//	};
//		[System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet=System.Runtime.InteropServices.CharSet.Ansi)]
//	public struct SockAddr {
//		
//		/// u_short->unsigned short
//		public ushort Family; // address family, AF_xxx
//		
//		/// char[14]  // 14 bytes of protocol address
//		[System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst=14)]
//		public string Data;
//	}
	
    public static class Native
    {

		public static IPAddress ConvertSockAddrPtrToIPAddress(IntPtr sockAddrPtr)
		{
			SockAddr sockAddr = (SockAddr)Marshal.PtrToStructure(sockAddrPtr, typeof(SockAddr));
			switch ((SockAddrFamily)sockAddr.Family)
			{
			case SockAddrFamily.Inet:
			{
				SockAddrIn sockAddrIn = (SockAddrIn)Marshal.PtrToStructure(sockAddrPtr, typeof(SockAddrIn));
				return new IPAddress(sockAddrIn.Addr);
			}
			case SockAddrFamily.Inet6:
			{
				SockAddrIn6 sockAddrIn6 = (SockAddrIn6)Marshal.PtrToStructure(sockAddrPtr, typeof(SockAddrIn6));
				return new IPAddress(sockAddrIn6.Addr);
			}
			default:
//				throw new Exception(string.Format("Non-IP address family: {0}", sockAddr.Family));
				SockAddrIn sockAddrIn2 = (SockAddrIn)Marshal.PtrToStructure(sockAddrPtr, typeof(SockAddrIn));
				System.Console.WriteLine(string.Format ("Non-IP address family: {0}", sockAddr.Family) + " " + sockAddrIn2.Port.ToString() + " " + sockAddrIn2.Addr);

				return new IPAddress(sockAddrIn2.Addr);
			}
		}
		// DNSServiceBrowse
		public delegate void DNSServiceBrowseReply(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex,
		                                           ServiceError errorCode, IntPtr serviceName, string regtype, string replyDomain,
		                                           IntPtr context);
	
		public delegate void DNSServiceResolveReply(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex,
		                                            ServiceError errorCode, IntPtr fullname, string hosttarget, ushort port, ushort txtLen,
		                                            IntPtr txtRecord, IntPtr browse);
		public delegate void DNSServiceGetAddrInfoReply( ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex,
		                                                ServiceError errorCode,IntPtr hostname, IntPtr address,
		                                                uint ttl, IntPtr context );
		public delegate void DNSServiceRegisterReply(ServiceRef sdRef, ServiceFlags flags, ServiceError errorCode,
		                                             IntPtr name, string regtype, string domain, IntPtr context);
		public delegate void DNSServiceQueryRecordReply(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex,
		                                                ServiceError errorCode, string fullname, ServiceType rrtype, ServiceClass rrclass, ushort rdlen, 
		                                                IntPtr rdata, uint ttl, IntPtr context);


#if UNITY_EDITOR
		[DllImport("DNSSDWrapper")] //calls select() mo properly.
		public static extern int DNSServiceSetDispatchQueueMainQueue(IntPtr sdRef);
		[DllImport("libc")]
        public static extern void DNSServiceRefDeallocate(IntPtr sdRef);
        
        [DllImport("libc")]
        public static extern ServiceError DNSServiceProcessResult(IntPtr sdRef);
       
        [DllImport("libc")]
        public static extern int DNSServiceRefSockFD(IntPtr sdRef);
        
        [DllImport("libc")]
        public static extern ServiceError DNSServiceCreateConnection(out ServiceRef sdRef);
        
        [DllImport("libc")]
        public static extern ServiceError DNSServiceBrowse(out ServiceRef sdRef, ServiceFlags flags,
            uint interfaceIndex, string regtype, string domain, DNSServiceBrowseReply callBack, 
            IntPtr context);
        
        // DNSServiceResolve
		
            
        [DllImport("libc")]
        public static extern ServiceError DNSServiceResolve(out ServiceRef sdRef, ServiceFlags flags,
            uint interfaceIndex, string name, string regtype, string domain, DNSServiceResolveReply callBack,
		                                                    IntPtr context);
        

		[DllImport("libc")]
		public static extern ServiceError DNSServiceGetAddrInfo (
				out  ServiceRef sdRef,ServiceFlags flags,uint interfaceIndex,
			byte protocol,string hostname,DNSServiceGetAddrInfoReply callBack,IntPtr context /* may be NULL */);

        // DNSServiceRegister
    
    
        [DllImport("libc")]
        public static extern ServiceError DNSServiceRegister(out ServiceRef sdRef, ServiceFlags flags,
            uint interfaceIndex, string name, string regtype, string domain, string host, ushort port,
            ushort txtLen, byte [] txtRecord, DNSServiceRegisterReply callBack, IntPtr context);

        // DNSServiceQueryRecord
        
        
        [DllImport("libc")]
        public static extern ServiceError DNSServiceQueryRecord(out ServiceRef sdRef, ServiceFlags flags, 
            uint interfaceIndex, string fullname, ServiceType rrtype, ServiceClass rrclass, 
            DNSServiceQueryRecordReply callBack, IntPtr context);
        
        // TXT Record Handling
        
        [DllImport("libc")]
        public static extern void TXTRecordCreate( IntPtr txtRecord, ushort bufferLen, IntPtr buffer);
    
        [DllImport("libc")]
        public static extern void TXTRecordDeallocate(IntPtr txtRecord);
    
        [DllImport("libc")]
        public static extern ServiceError TXTRecordGetItemAtIndex(ushort txtLen, IntPtr txtRecord,
            ushort index, ushort keyBufLen, byte [] key, out byte valueLen, out IntPtr value);
            
        [DllImport("libc")]
        public static extern ServiceError TXTRecordSetValue(IntPtr txtRecord, byte [] key, 
            sbyte valueSize, byte [] value);
            
        [DllImport("libc")]
        public static extern ServiceError TXTRecordRemoveValue(IntPtr txtRecord, byte [] key);
        
        [DllImport("libc")]
        public static extern ushort TXTRecordGetLength(IntPtr txtRecord);
        
        [DllImport("libc")]
        public static extern IntPtr TXTRecordGetBytesPtr(IntPtr txtRecord);
        
        [DllImport("libc")]
        public static extern ushort TXTRecordGetCount(ushort txtLen, IntPtr txtRecord);
		[DllImport("libc")]
		public static extern int getpid();

#elif UNITY_IPHONE

		[DllImport("__Internal")] //calls select() mo properly.
		public static extern int DNSServiceSetDispatchQueueMainQueue(IntPtr sdRef);
		[DllImport("__Internal")]
		public static extern void DNSServiceRefDeallocate(IntPtr sdRef);
		
		[DllImport("__Internal")]
		public static extern ServiceError DNSServiceProcessResult(IntPtr sdRef);
		
		[DllImport("__Internal")]
		public static extern int DNSServiceRefSockFD(IntPtr sdRef);
		
		[DllImport("__Internal")]
		public static extern ServiceError DNSServiceCreateConnection(out ServiceRef sdRef);


		[DllImport("__Internal")]
		public static extern ServiceError DNSServiceBrowse(out ServiceRef sdRef, ServiceFlags flags,
		                                                   uint interfaceIndex, string regtype, string domain, DNSServiceBrowseReply callBack, 
		                                                   IntPtr context);
		//[MarshalAs(UnmanagedType.IUnknown)]

		[DllImport("__Internal")]
		public static extern ServiceError DNSServiceResolve(out ServiceRef sdRef, ServiceFlags flags,
		                                                    uint interfaceIndex, string name, string regtype, string domain, DNSServiceResolveReply callBack,
		                                                    IntPtr context);
		

		[DllImport("__Internal")]
		public static extern ServiceError DNSServiceGetAddrInfo (
			out  ServiceRef sdRef,ServiceFlags flags,uint interfaceIndex,
			byte protocol,string hostname,DNSServiceGetAddrInfoReply callBack,IntPtr context /* may be NULL */);
		
		// DNSServiceRegister
		

		[DllImport("__Internal")]
		public static extern ServiceError DNSServiceRegister(out ServiceRef sdRef, ServiceFlags flags,
		                                                     uint interfaceIndex, string name, string regtype, string domain, string host, ushort port,
		                                                     ushort txtLen, byte [] txtRecord, DNSServiceRegisterReply callBack, IntPtr context);
		
		// DNSServiceQueryRecord
		

		[DllImport("__Internal")]
		public static extern ServiceError DNSServiceQueryRecord(out ServiceRef sdRef, ServiceFlags flags, 
		                                                        uint interfaceIndex, string fullname, ServiceType rrtype, ServiceClass rrclass, 
		                                                        DNSServiceQueryRecordReply callBack, IntPtr context);
		
		// TXT Record Handling
		
		[DllImport("__Internal")]
		public static extern void TXTRecordCreate( IntPtr txtRecord, ushort bufferLen, IntPtr buffer);
		
		[DllImport("__Internal")]
		public static extern void TXTRecordDeallocate(IntPtr txtRecord);
		
		[DllImport("__Internal")]
		public static extern ServiceError TXTRecordGetItemAtIndex(ushort txtLen, IntPtr txtRecord,
		                                                          ushort index, ushort keyBufLen, byte [] key, out byte valueLen, out IntPtr value);
		
		[DllImport("__Internal")]
		public static extern ServiceError TXTRecordSetValue(IntPtr txtRecord, byte [] key, 
		                                                    sbyte valueSize, byte [] value);
		
		[DllImport("__Internal")]
		public static extern ServiceError TXTRecordRemoveValue(IntPtr txtRecord, byte [] key);
		
		[DllImport("__Internal")]
		public static extern ushort TXTRecordGetLength(IntPtr txtRecord);
		
		[DllImport("__Internal")]
		public static extern IntPtr TXTRecordGetBytesPtr(IntPtr txtRecord);
		
		[DllImport("__Internal")]
		public static extern ushort TXTRecordGetCount(ushort txtLen, IntPtr txtRecord);
		[DllImport("__Internal")]
		public static extern int getpid();
#else
		[DllImport("DNSSDWrapper")] //calls select() mo properly.
		public static extern int DNSServiceSetDispatchQueueMainQueue(IntPtr sdRef);
		[DllImport("libc")]
		public static extern void DNSServiceRefDeallocate(IntPtr sdRef);
		
		[DllImport("libc")]
		public static extern ServiceError DNSServiceProcessResult(IntPtr sdRef);
		
		[DllImport("libc")]
		public static extern int DNSServiceRefSockFD(IntPtr sdRef);
		
		[DllImport("libc")]
		public static extern ServiceError DNSServiceCreateConnection(out ServiceRef sdRef);
		
		// DNSServiceBrowse

		[DllImport("libc")]
		public static extern ServiceError DNSServiceBrowse(out ServiceRef sdRef, ServiceFlags flags,
		                                                   uint interfaceIndex, string regtype, string domain, DNSServiceBrowseReply callBack, 
		                                                   IntPtr context);
		
		// DNSServiceResolve
		
	
		[DllImport("libc")]
		public static extern ServiceError DNSServiceResolve(out ServiceRef sdRef, ServiceFlags flags,
		                                                    uint interfaceIndex, string name, string regtype, string domain, DNSServiceResolveReply callBack,
		                                                    IntPtr context);
		

		[DllImport("libc")]
		public static extern ServiceError DNSServiceGetAddrInfo (
			out  ServiceRef sdRef,ServiceFlags flags,uint interfaceIndex,
			byte protocol,string hostname,DNSServiceGetAddrInfoReply callBack,IntPtr context /* may be NULL */);
		
		// DNSServiceRegister
		

		[DllImport("libc")]
		public static extern ServiceError DNSServiceRegister(out ServiceRef sdRef, ServiceFlags flags,
		                                                     uint interfaceIndex, string name, string regtype, string domain, string host, ushort port,
		                                                     ushort txtLen, byte [] txtRecord, DNSServiceRegisterReply callBack, IntPtr context);
		
		// DNSServiceQueryRecord
		
		[DllImport("libc")]
		public static extern ServiceError DNSServiceQueryRecord(out ServiceRef sdRef, ServiceFlags flags, 
		                                                        uint interfaceIndex, string fullname, ServiceType rrtype, ServiceClass rrclass, 
		                                                        DNSServiceQueryRecordReply callBack, IntPtr context);
		
		// TXT Record Handling
		
		[DllImport("libc")]
		public static extern void TXTRecordCreate( IntPtr txtRecord, ushort bufferLen, IntPtr buffer);
		
		[DllImport("libc")]
		public static extern void TXTRecordDeallocate(IntPtr txtRecord);
		
		[DllImport("libc")]
		public static extern ServiceError TXTRecordGetItemAtIndex(ushort txtLen, IntPtr txtRecord,
		                                                          ushort index, ushort keyBufLen, byte [] key, out byte valueLen, out IntPtr value);
		
		[DllImport("libc")]
		public static extern ServiceError TXTRecordSetValue(IntPtr txtRecord, byte [] key, 
		                                                    sbyte valueSize, byte [] value);
		
		[DllImport("libc")]
		public static extern ServiceError TXTRecordRemoveValue(IntPtr txtRecord, byte [] key);
		
		[DllImport("libc")]
		public static extern ushort TXTRecordGetLength(IntPtr txtRecord);
		
		[DllImport("libc")]
		public static extern IntPtr TXTRecordGetBytesPtr(IntPtr txtRecord);
		
		[DllImport("libc")]
		public static extern ushort TXTRecordGetCount(ushort txtLen, IntPtr txtRecord);
#endif


		//BC note: the nice thing about these static callbacks/the reason why I'm using static instead of instance callback methods,
		//is that you can validate that the context is valid from an async callback (if i.e., serviceBrowserInstance == null), which is relevant in an environment like Unity
		[MonoPInvokeCallback (typeof(DNSServiceBrowseReply))]
		public static void OnBrowseReply(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex, ServiceError errorCode, 
		                                 IntPtr serviceName, string regtype, string replyDomain, IntPtr context)
			
		{
			
			try{
				GCHandle handle = GCHandle.FromIntPtr(context);
				ServiceBrowser serviceBrowserInstance = (ServiceBrowser)handle.Target;
				
				if(serviceBrowserInstance != null){
					serviceBrowserInstance.OnBrowseReplyInstance(sdRef, flags, interfaceIndex, errorCode, serviceName, regtype, replyDomain, context);
				}				
			}
			catch(System.ArgumentException e){
				System.Console.WriteLine ("SHIT WAS PROBABLY ALREADY FREED OnBrowseReply! dbl free? :: "+ e.ToString()); 
			}
		}

		[MonoPInvokeCallback (typeof(DNSServiceResolveReply))]
		public static void OnResolveReply(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex,
		                                  ServiceError errorCode, IntPtr fullname, string hosttarget, ushort port, ushort txtLen,
		                                  IntPtr txtRecord, IntPtr browse)
		{
			try{
				GCHandle handle = GCHandle.FromIntPtr(browse);
				BrowseService browseSvc = (BrowseService)handle.Target;
				
				
				if(browseSvc != null){
					browseSvc.OnResolveReplyInstance(sdRef, flags, interfaceIndex, errorCode, fullname, hosttarget, port, txtLen, txtRecord, browse);
				}
				
			}
			catch(System.ArgumentException e){
				System.Console.WriteLine ("SHIT WAS PROBABLY ALREADY FREED! dbl free? :: "+ e.ToString()); 
			}
			
		}
		
		[MonoPInvokeCallback (typeof(DNSServiceQueryRecordReply))]
		public static void OnQueryRecordReply(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex,
		                                      ServiceError errorCode, string fullname, ServiceType rrtype, ServiceClass rrclass, ushort rdlen, 
		                                      IntPtr rdata, uint ttl, IntPtr browse)
		{
			try{
				GCHandle handle = GCHandle.FromIntPtr(browse);
				BrowseService browseSvc = (BrowseService)handle.Target;
				
				
				if(browseSvc != null){
					browseSvc.OnQueryRecordReplyInstance(sdRef, flags, interfaceIndex, errorCode, fullname, rrtype, rrclass, rdlen, rdata, ttl, browse);
				}
				
			}
			catch(System.ArgumentException e){
				System.Console.WriteLine ("SHIT WAS PROBABLY ALREADY FREED OnQueryRecordReply! dbl free? :: "+ e.ToString()); 
			}
		}

		[MonoPInvokeCallback (typeof(DNSServiceRegisterReply))]
		public static void OnRegisterReply(ServiceRef sdRef, ServiceFlags flags, ServiceError errorCode,
		                                   IntPtr name, string regtype, string domain, IntPtr context)
		{
			try{
				GCHandle handle = GCHandle.FromIntPtr(context);
				RegisterService registerSrv = (RegisterService)handle.Target;
				
				
				if(registerSrv != null){
					registerSrv.OnRegisterReplyInstance(sdRef, flags, errorCode, name, regtype, domain, context);
				}
				
			}
			catch(System.ArgumentException e){
				System.Console.WriteLine ("SHIT WAS PROBABLY ALREADY FREED OnQueryRecordReply! dbl free? :: "+ e.ToString()); 
			}
			
		}


        public static string Utf8toString(IntPtr ptr) {
            int len = 0;
            while (Marshal.ReadByte(ptr, len) != 0) {
                len++;
            }
            byte[] raw = new byte[len];
            Marshal.Copy(ptr, raw, 0, len);
            return Encoding.UTF8.GetString(raw);
        }
    }
}
