//
// BrowseService.cs
//
// Authors:
//    Aaron Bockover  <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;

namespace Mono.Zeroconf.Providers.Bonjour
{
    public sealed class BrowseService : Service, IResolvableService
    {
        private bool is_resolved = false;
		private bool isStopped = false;
		private Native.DNSServiceGetAddrInfoReply addr_info_reply_handler;
        private Native.DNSServiceResolveReply resolve_reply_handler;
        private Native.DNSServiceQueryRecordReply query_record_reply_handler;
        
        public event ServiceResolvedEventHandler Resolved;
		public object quitLockObj = new object();
		private ServiceRef resolveRef;
		private bool needsResolve = false;
		private ServiceRef queryRef;
		private bool needsQuery = false;
		private volatile bool is_stopping_app = false;

		private GCHandle gcHandle;

		public void Stop(){
			if(!isStopped){
				//isStopped makes sure you only Free the gcHandle once
				isStopped = true;
				System.Console.WriteLine("stopping the BrowseService for : " + Name);
				deallocateResolveRef();
				deallocateQueryRef();
				is_stopping_app = true;

				gcHandle.Free ();
			}
		}

        public BrowseService()
        {
            SetupCallbacks();
        }
        
        public BrowseService(string name, string replyDomain, string regtype) : base(name, replyDomain, regtype)
        {
            SetupCallbacks();
        }
        
        private void SetupCallbacks()
        {
			gcHandle = GCHandle.Alloc (this);
			isStopped = false;
			System.Console.WriteLine ("STARTING HERE AT: " + name);
			is_stopping_app = false;
			needsResolve = false;
			needsQuery = false;
			resolve_reply_handler = new Native.DNSServiceResolveReply(Native.OnResolveReply);
			query_record_reply_handler = new Native.DNSServiceQueryRecordReply(Native.OnQueryRecordReply);
//			addr_info_reply_handler = new Native.DNSServiceGetAddrInfoReply(OnAddrInfoReply);
        }

        public void Resolve()
        {
            Resolve(false);
        }
        
        public void Resolve(bool requery)
        {
			lock(quitLockObj){

				deallocateResolveRef();
	        
	            is_resolved = false;
	            
	            if(requery) {
	                InterfaceIndex = 0;
	            }
							


				//BC NOTE:
				//okay, proper deallocation is complicated. I think this is correct:
				//if you attempt to resolve, and exit the app before you get a response, you need to deallocate the attempt's sdRef (called resolveRef here)
				//if you attempt to resolve, and get a response, you need to deallocate the response's sdRef (and not the attempt)

//		             ServiceRef sd_ref;
//				ServiceError error = Native.DNSServiceGetAddrInfo (out resolveRef, this.Flags,this.InterfaceIndex,(byte)0,
//				                                                   fullname ,addr_info_reply_handler,IntPtr.Zero);



				ServiceError error = Native.DNSServiceResolve(out resolveRef, ServiceFlags.BackgroundTrafficClass & ServiceFlags.Timeout, 
	                              InterfaceIndex, Name, RegType, ReplyDomain, resolve_reply_handler, GCHandle.ToIntPtr(gcHandle));
                

	            if(error != ServiceError.NoError) {
	                throw new ServiceErrorException(error);
				}

				needsResolve = true;
				resolveRef.Process();
//					sd_ref.Deallocate();
				
//				System.Console.WriteLine("TRYING RESOLVE!" + Name + " plus " + resolveRef.Raw );
				
			}//end lock
		}


		//incomplemete/untested for RefreshTxtRecord
        public void RefreshTxtRecord()
        {
            // Should probably make this async?
			if(is_stopping_app)
				return;

			if(quitLockObj == null){
//				System.Console.WriteLine ("NULL QUITLOCK MUTEX");
				return;
			}
			lock(quitLockObj){
			   	ServiceRef sd_ref;
	            ServiceError error = Native.DNSServiceQueryRecord(out sd_ref, ServiceFlags.None, 0,
	                fullname, ServiceType.TXT, ServiceClass.IN, query_record_reply_handler,  GCHandle.ToIntPtr(gcHandle));
	                
	            if(error != ServiceError.NoError) {
	                throw new ServiceErrorException(error);
	            }
//				System.Console.WriteLine ("from text record refresh: process.");

				if(sd_ref.Raw != IntPtr.Zero)
					sd_ref.Process();
			}
		}

		private void deallocateQueryRef(){
			if(needsQuery && queryRef.Raw != IntPtr.Zero){
				System.Console.WriteLine("quitting query" + queryRef.Raw);
				queryRef.Deallocate();
				needsQuery = false;
			}
		}

		private void deallocateResolveRef(){
			if(needsResolve && resolveRef.Raw != IntPtr.Zero){
				System.Console.WriteLine("quitting resolve" + resolveRef.Raw);

				resolveRef.Deallocate();
				needsResolve = false;
			}
		}




        public void OnResolveReplyInstance(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex,
            ServiceError errorCode, IntPtr fullname, string hosttarget, ushort port, ushort txtLen,
            IntPtr txtRecord, IntPtr contex)
        {
			System.Console.WriteLine ("from OnResolveReply: "+ Name );

			if(is_stopping_app || errorCode != ServiceError.NoError){
				System.Console.WriteLine ("STOPPING APP RESOLVEREPLY");
				sdRef.Deallocate();
				needsResolve = false;
				return;
			}
			if(quitLockObj == null){
//				System.Console.WriteLine ("NULL QUITLOCK MUTEX");
				return;
			}


			
			lock(quitLockObj){
	            is_resolved = true;

	            InterfaceIndex = interfaceIndex;
	            FullName = Native.Utf8toString(fullname);
//				System.Console.WriteLine ("got a response for RESOLVE reply: " +Name + " " + txtLen) ;
				this.port = (ushort)IPAddress.NetworkToHostOrder((short)port);
	            TxtRecord = new TxtRecord(txtLen, txtRecord);
	            this.hosttarget = hosttarget;


				//deallocate the sdRef
				sdRef.Deallocate();
				needsResolve = false;
				// Run an A query to resolve the IP address
//	            ServiceRef sd_ref;
	            
	            if (AddressProtocol == AddressProtocol.Any || AddressProtocol == AddressProtocol.IPv4) {
					ServiceError error = Native.DNSServiceQueryRecord(out queryRef, ServiceFlags.None, interfaceIndex,
	                    hosttarget, ServiceType.A, ServiceClass.IN, query_record_reply_handler,  GCHandle.ToIntPtr(gcHandle));
					if(error != ServiceError.NoError) {
						System.Console.WriteLine ("EXCEPTION IS : " + error + " " + interfaceIndex + " "  + hosttarget + (int)GCHandle.ToIntPtr(gcHandle) );
	                    throw new ServiceErrorException(error);	
	                }

					if(queryRef.Raw != IntPtr.Zero)
						queryRef.Process();

					//mark the queryRef as 'in flight'
					needsQuery = true;
				}
	            
				if (AddressProtocol == AddressProtocol.IPv6){
				//TODO: this is a stupid fucking bug. FUCK YOU !@#$%^&%$#@#$%^&%$#@$%^&^%$#@
	//            if (AddressProtocol == AddressProtocol.Any || AddressProtocol == AddressProtocol.IPv6) {
					ServiceError error = Native.DNSServiceQueryRecord(out queryRef, ServiceFlags.None, interfaceIndex,
	                    hosttarget, ServiceType.AAAA, ServiceClass.IN, query_record_reply_handler,  GCHandle.ToIntPtr(gcHandle));
	                
	                if(error != ServiceError.NoError) {
	                    throw new ServiceErrorException(error);
	                }
//					System.Console.WriteLine ("from ipv6 resolve reply: process.");

					if(queryRef.Raw != IntPtr.Zero)
						queryRef.Process();

					//mark the queryRef as 'in flight'
					needsQuery = true;

				}
			}
        }


		//incomplete implementation for OnAddrInfoReply
		private void OnAddrInfoReply(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex,ServiceError errorCode,IntPtr rdata,
		                             IntPtr rawaddr, uint ttl, IntPtr context )
		{


			if(is_stopping_app){
				sdRef.Deallocate();
				//needsAddrInfoReply = false;
				return;
			}

			string fullNamePlz = Native.Utf8toString(rdata);

//			byte[] bytes = Encoding.Default.GetBytes(rdata);
//			myString = Encoding.UTF8.GetString(bytes);
			IPAddress address = Native.ConvertSockAddrPtrToIPAddress(rawaddr);
			System.Console.WriteLine ("error: " + errorCode + " data : " + address.ToString() + ", ttl: " + ttl + " " + "hostname: " + fullNamePlz  );


			deallocateResolveRef();
		}
     
        public void OnQueryRecordReplyInstance(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex,
            ServiceError errorCode, string fullname, ServiceType rrtype, ServiceClass rrclass, ushort rdlen, 
            IntPtr rdata, uint ttl, IntPtr context)
        {
			if(is_stopping_app || errorCode != ServiceError.NoError){
				System.Console.WriteLine ("STOPPING APP");
				sdRef.Deallocate();
				needsQuery = false;
				return;
			}

			if(quitLockObj == null){
				System.Console.WriteLine ("NULL QUITLOCK MUTEX");
				return;
			}
			
			
			//			System.Console.WriteLine ("got a response for query record reply: " +Name + " " + rdlen) ;
			lock(quitLockObj){
				switch(rrtype) {
	                case ServiceType.A:
	                    IPAddress address;

	                    if(rdlen == 4) {   
	                        // ~4.5 times faster than Marshal.Copy into byte[4]
	                        uint address_raw = (uint)(Marshal.ReadByte (rdata, 3) << 24);
	                        address_raw |= (uint)(Marshal.ReadByte (rdata, 2) << 16);
	                        address_raw |= (uint)(Marshal.ReadByte (rdata, 1) << 8);
	                        address_raw |= (uint)Marshal.ReadByte (rdata, 0);

	                        address = new IPAddress(address_raw);
	                    } else if(rdlen == 16) {
	                        byte [] address_raw = new byte[rdlen];
	                        Marshal.Copy(rdata, address_raw, 0, rdlen);
	                        address = new IPAddress(address_raw, interfaceIndex);
	                    } else {
	                        break;
	                    }

	                    if(hostentry == null) {
	                        hostentry = new IPHostEntry();
	                        hostentry.HostName = hosttarget;
	                    }
	                    
	                    if(hostentry.AddressList != null) {
	                        ArrayList list = new ArrayList(hostentry.AddressList);
	                        list.Add(address);
	                        hostentry.AddressList = list.ToArray(typeof(IPAddress)) as IPAddress [];
	                    } else {
	                        hostentry.AddressList = new IPAddress [] { address };
	                    }
	                    
	                    ServiceResolvedEventHandler handler = Resolved;
	                    if(handler != null) {
	                        handler(this, new ServiceResolvedEventArgs(this));
	                    }
	                    
	                    break;
	                case ServiceType.TXT:
	                    if(TxtRecord != null) {
	                        TxtRecord.Dispose();
	                    }
	            
	                    TxtRecord = new TxtRecord(rdlen, rdata);
	                    break;
	                default:
	                    break;
	            }
				sdRef.Deallocate();
				needsQuery = false;
			}
        }
        
        public bool IsResolved {
            get { return is_resolved; }
        }
    }
}

