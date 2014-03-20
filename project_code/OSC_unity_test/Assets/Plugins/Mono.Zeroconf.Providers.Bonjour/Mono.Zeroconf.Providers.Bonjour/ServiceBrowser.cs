//
// ServiceBrowser.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.InteropServices;

namespace Mono.Zeroconf.Providers.Bonjour
{
    public class ServiceBrowseEventArgs : Mono.Zeroconf.ServiceBrowseEventArgs
    {
        private bool more_coming;
        
        public ServiceBrowseEventArgs(BrowseService service, bool moreComing) : base(service)
        {
            this.more_coming = moreComing;
        }
        
        public bool MoreComing {
            get { return more_coming; }
        }
    }
    
    public class ServiceBrowser : IServiceBrowser, IEnumerable// IDisposable
    {


		private object _quitLockObj = new object();
        private uint interface_index;
        private AddressProtocol address_protocol;
        private string regtype;
        private string domain;
        private volatile bool isDisposed = false;
		
        private ServiceRef sd_ref = ServiceRef.Zero;
        private Dictionary<string, IResolvableService> service_table = new Dictionary<string, IResolvableService> ();
        
        private Native.DNSServiceBrowseReply browse_reply_handler;
        
        private Thread thread;
		private GCHandle gcHandle;

        public event ServiceBrowseEventHandler ServiceAdded;
        public event ServiceBrowseEventHandler ServiceRemoved;
        
        public ServiceBrowser()
        {

			System.Console.WriteLine ("ALLOCING THE SERVICE BROWSER: ");
			isDisposed = false;
			gcHandle = GCHandle.Alloc (this);
			browse_reply_handler = new  Native.DNSServiceBrowseReply(Native.OnBrowseReply);
        }
        
        public void Browse (uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain)
        {
            Configure(interfaceIndex, addressProtocol, regtype, domain);
            StartAsync();
        }

        public void Configure(uint interfaceIndex, AddressProtocol addressProtocol, string regtype, string domain)
        {
            this.interface_index = interfaceIndex;
            this.address_protocol = addressProtocol;
            this.regtype = regtype;
            this.domain = domain;
            
            if(regtype == null) {
                throw new ArgumentNullException("regtype");
            }
        }
        
        private void Start(bool @async)
        {
            if(thread != null) {
                throw new InvalidOperationException("ServiceBrowser is already started");
            }
            
            if(@async) {
                thread = new Thread(new ThreadStart(ThreadedStart));
                thread.IsBackground = true;
				thread.Name = "ServiceBrowserThread";
                thread.Start();
            } else {
                ProcessStart();
            }
        }
        
        public void Start()
        {
            Start(false);
        }
        
        public void StartAsync()
        {
            Start(true);
        }
        
        private void ThreadedStart()
        {
            try {
                ProcessStart();
            } catch(ThreadAbortException e) {
				System.Console.WriteLine("THREAD ABORT ISSUE!: " + e.ToString());
                thread.Abort();
				thread = null;
            }
            
//            thread = null;
        }

        private void ProcessStart()
        {
			lock(_quitLockObj){

				ServiceError error = ServiceError.NoError;
				try{
					error = Native.DNSServiceBrowse(out sd_ref, ServiceFlags.Default,
				                                             interface_index, regtype,  domain, browse_reply_handler, GCHandle.ToIntPtr(gcHandle));
				}
				catch(Exception e){
					System.Console.WriteLine("GOT EXCEPTION: " + e.ToString());
				}

	            if(error != ServiceError.NoError) {
					System.Console.WriteLine ("error at Native.DNSServiceBrowse is " + error.ToString());
	                throw new ServiceErrorException(error);
	            }
				else{
					sd_ref.Process();
				}
				
			}
        }
        
        public void Stop()
        {
			System.Console.WriteLine ("SHOULD BE STOPPING HERE!");
			if(!isDisposed){
				gcHandle.Free();

				isDisposed = true;
				//BC (important) edit: lock around this object that shares with all the BrowseServices.
				//stop may get called from a different method while you're halfway through a BrowseServices callback.
				// this way you can't modify some shit from a function you're halfway through.
				// also, calling stop on each of the services prevents you from being able to do anything if you get a (later) callback.
				lock(_quitLockObj){
					lock (this) {
						System.Console.WriteLine ("service table count is  " + service_table.Count);
						foreach (BrowseService service in service_table.Values) {
							service.Stop ();
						}
					}
					
					if( sd_ref != ServiceRef.Zero) {
						sd_ref.Deallocate();
		                sd_ref = ServiceRef.Zero;
		            }
		            
		            if(thread != null) {
						if(!thread.Join(100)){
							System.Console.WriteLine ("aborting thread. took too long on ServiceBrowser.");
							thread.Abort();
						}

	//					thread = null;
		            }
				}
			}
        }


//this method is bad and you should feel bad. 
//don't fuck with unity's GC plz.

//		protected virtual void Dispose(bool disposing){
//			if(!isDisposed){
//				if(disposing){
//					Stop();
//					System.Console.WriteLine ("truly disposing of the shit");
//				}
//				isDisposed = true;
//			}
//		}
//
//        public void Dispose()
//        {
//			Dispose(true);
//			GC.SuppressFinalize(this);
//        }
        
        public IEnumerator<Mono.Zeroconf.IResolvableService> GetEnumerator ()
        {
            lock (this) {
                foreach (IResolvableService service in service_table.Values) {
                    yield return service;
                }
            }
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator ()
        {
            return GetEnumerator ();
        }

	

		public void OnBrowseReplyInstance(ServiceRef sdRef, ServiceFlags flags, uint interfaceIndex, ServiceError errorCode, 
            IntPtr serviceName, string regtype, string replyDomain, IntPtr context)
        {
			// if( sd_ref != ServiceRef.Zero && (flags & ServiceFlags.MoreComing) == 0) {
			// 	sd_ref.Deallocate();
			// 	sd_ref = ServiceRef.Zero;
			// }

			//
			string name = Native.Utf8toString(serviceName);
            BrowseService service = new BrowseService(name, replyDomain, regtype);
			service.quitLockObj = _quitLockObj;
            service.Flags = flags;
            service.InterfaceIndex = interfaceIndex;
            service.AddressProtocol = address_protocol;
            
//			System.Console.WriteLine ("the name is"  + name);
            ServiceBrowseEventArgs args = new ServiceBrowseEventArgs(
                service, (flags & ServiceFlags.MoreComing) != 0);
            
            if((flags & ServiceFlags.Add) != 0) {
                lock (service_table) {
                    if (service_table.ContainsKey (name)) {
                        service_table[name] = service;
                    } else {
                        service_table.Add (name, service);
                    }
                }
                
                ServiceBrowseEventHandler handler = ServiceAdded;
                if(handler != null) {
                    handler(this, args);
                }
            } else {
                lock (service_table) {
                    if (service_table.ContainsKey (name)) {
						BrowseService svc = (BrowseService)service_table[name];
						svc.Stop();
                        service_table.Remove (name);
                    }
                }
                
                ServiceBrowseEventHandler handler = ServiceRemoved;
                if(handler != null) {
                    handler(this, args);
                }
            }
        }
    }
}
