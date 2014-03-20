//
// RegisterService.cs
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
using System.Threading;
using System.Runtime.InteropServices;
using System.Text;

namespace Mono.Zeroconf.Providers.Bonjour
{
    public sealed class RegisterService : Service, IRegisterService//, IDisposable
    {
		private object _quitLockObj = new object();
        private Thread thread;
        private ServiceRef sd_ref;
        private bool auto_rename = true;
    	private bool isDisposing = false;
		private bool isInit = false;
        private Native.DNSServiceRegisterReply register_reply_handler;
		private GCHandle gcHandle;

        public event RegisterServiceEventHandler Response;
    
        public RegisterService()
        {
			SetupCallback();
        }
        
        public RegisterService(string name, string replyDomain, string regtype) : base(name, replyDomain, regtype)
        {
			SetupCallback();
        }
        
        private void SetupCallback()
        {
			gcHandle = GCHandle.Alloc (this);
            register_reply_handler = new Native.DNSServiceRegisterReply(Native.OnRegisterReply);
        }
        
        public void Register()
        {
            Register(true);
        }
    
        public void Register(bool @async)
        {
			lock(_quitLockObj){
	            if(thread != null) {
	                throw new InvalidOperationException("RegisterService registration already in process");
	            }
	            
				if(isDisposing){
					System.Console.WriteLine("ZeroConf could not start registration: registration instance in process of being disposed.");
					return;
				}
	            if(@async) {
	                thread = new Thread(new ThreadStart(ThreadedRegister));
	                thread.IsBackground = true;
					thread.Name = "Register Svc Thread";
	                thread.Start();
					isDisposing = false;
	            } else {
	                ProcessRegister();
	            }
			}
        }
        
        public void RegisterSync()
        {
            Register(false);
        }
    
        private void ThreadedRegister()
        {
            try {
                ProcessRegister();
            } catch(ThreadAbortException) {
                Thread.ResetAbort();
            }
            
            thread = null;
        }
    
        public void ProcessRegister()
        {
            ushort txt_rec_length = 0;
            byte [] txt_rec = null;

			if(TxtRecord != null) {
                txt_rec_length = ((TxtRecord)TxtRecord.BaseRecord).RawLength;
                txt_rec = new byte[txt_rec_length];
                Marshal.Copy(((TxtRecord)TxtRecord.BaseRecord).RawBytes, txt_rec, 0, txt_rec_length);
            }
            
            ServiceError error = Native.DNSServiceRegister(out sd_ref, 
                (auto_rename ? ServiceFlags.None : ServiceFlags.NoAutoRename) , InterfaceIndex,
                Name, RegType, ReplyDomain, HostTarget, (ushort)IPAddress.HostToNetworkOrder((short)port), txt_rec_length, txt_rec,
                register_reply_handler, GCHandle.ToIntPtr(gcHandle));
			
			if(error != ServiceError.NoError) {
                throw new ServiceErrorException(error);
            }

			isInit = true;
       		sd_ref.Process();
			System.Console.WriteLine("registering new service:" + Name);
		}
        
		public void Stop(){


			gcHandle.Free();
			System.Console.WriteLine ("ENDING!");
			if(!isDisposing){
				isDisposing = true;

				lock(_quitLockObj){
						if(thread != null) {
							thread.Join();
							thread = null;
						}
//						else
//						{
//							System.Console.WriteLine ("thread had already ended.");
//						}

						if(isInit)
						{
							sd_ref.Deallocate();
							isInit = false;
						}
					}
			}
		}
		
		//        public void Dispose()
		//        {
		//			if(!isDisposing){
//			
//	            if(thread != null) {
//	                thread.Join();
//	                thread = null;
//	            }
//	            
//	            sd_ref.Deallocate();
//			}
//        }


        public void OnRegisterReplyInstance(ServiceRef sdRef, ServiceFlags flags, ServiceError errorCode,
            IntPtr name, string regtype, string domain, IntPtr context)
        {
            RegisterServiceEventArgs args = new RegisterServiceEventArgs();
            
            args.Service = this;
            args.IsRegistered = false;
            args.ServiceError = (ServiceErrorCode)errorCode;
            
            if(errorCode == ServiceError.NoError) {
                Name = Native.Utf8toString(name);
                RegType = regtype;
                ReplyDomain = domain;
                args.IsRegistered = true;
//				System.Console.WriteLine ("RegisterService: NO ERROR HERE!" + errorCode + " sdRef: " + (int) sdRef.Raw);

            }
			else{
//				System.Console.WriteLine ("RegisterService: ERROR HERE!" + errorCode + " sdRef: " + (int) sdRef.Raw);
				return;
			}
            
            RegisterServiceEventHandler handler = Response;
            if(handler != null) {
                handler(this, args);
            }
        }
        
        public bool AutoRename {
            get { return auto_rename; }
            set { auto_rename = value; }
        }
    }
}
