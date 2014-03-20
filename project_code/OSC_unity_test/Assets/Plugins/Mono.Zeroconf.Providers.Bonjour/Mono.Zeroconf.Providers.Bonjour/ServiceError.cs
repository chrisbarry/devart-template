//
// ServiceError.cs
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

namespace Mono.Zeroconf.Providers.Bonjour
{
    public enum ServiceError {
		NoError                   = 0,
		Unknown                   = -65537,       /* 0xFFFE FFFF */
		NoSuchName                = -65538,
		NoMemory                  = -65539,
		BadParam                  = -65540,
		BadReference              = -65541,
		BadState                  = -65542,
		BadFlags                  = -65543,
		Unsupported               = -65544,
		NotInitialized            = -65545,
		AlreadyRegistered         = -65547,
		NameConflict              = -65548,
		Invalid                   = -65549,
		Firewall                  = -65550,
		Incompatible              = -65551,        /* client library incompatible with daemon */
		BadInterfaceIndex         = -65552,
		Refused                   = -65553,
		NoSuchRecord              = -65554,
		NoAuth                    = -65555,
		NoSuchKey                 = -65556,
		NATTraversal              = -65557,
		DoubleNAT                 = -65558,
		BadTime                   = -65559,/* Codes up to here existed in Tiger */
		/*extra kDNSServiceErr_ errors:*/
		BadSig                    = -65560,
		BadKey                    = -65561,
		Transient                 = -65562,
		ServiceNotRunning         = -65563,  /* Background daemon not running */
		NATPortMappingUnsupported = -65564,  /* NAT doesn't support NAT-PMP or UPnP */
		NATPortMappingDisabled    = -65565,  /* NAT supports NAT-PMP or UPnP but it's disabled by the administrator */
		NoRouter                  = -65566,  /* No router currently configured (probably no network connectivity) */
		PollingMode               = -65567,
		Timeout                   = -65568

    }
}
