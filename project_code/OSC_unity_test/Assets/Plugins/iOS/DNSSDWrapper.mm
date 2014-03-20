#import <Foundation/Foundation.h>
#include <dns_sd.h>

extern "C" {
	int DNSServiceSetDispatchQueueMainQueue(void* tref)
	{
		@autoreleasepool {
			DNSServiceRef theRef = (DNSServiceRef)tref;
			if(theRef != nil)
				return DNSServiceSetDispatchQueue(theRef, dispatch_get_main_queue());
			
			return kDNSServiceErr_BadReference;
		}
	}
}




//old version using select() and DNSServiceProcessResult for posterity...

//#include <stdio.h>            // For stdout, stderr
//#include <string.h>            // For strlen(), strcpy(  ), bzero(  )
//#include <errno.h>            // For errno, EINTR
//#include <time.h>
//
//#ifdef _WIN32
//#include <process.h>
//typedef    int    pid_t;
//#define    getpid    _getpid
//#define    strcasecmp    _stricmp
//#define snprintf _snprintf
//#else
//#include <sys/time.h>        // For struct timeval
//#include <unistd.h>            // For getopt(  ) and optind
//#include <arpa/inet.h>        // For inet_addr(  )
//#endif

// Note: the select(  ) implementation on Windows (Winsock2)
//fails with any timeout much larger than this
//#define LONG_TIME 100000000

//    int dns_sd_fd = DNSServiceRefSockFD(serviceRef);
//	printf("fd is %i", dns_sd_fd);
//    int nfds = dns_sd_fd + 1;
//    fd_set readfds;
//    struct timeval tv;
//    int result;
//	bool stopNow = false;
//	
//    while (!stopNow)
//	{
//        FD_ZERO(&readfds);
//        FD_SET(dns_sd_fd, &readfds);
//        tv.tv_sec = timeOut;
//        tv.tv_usec = 0;
//		
//        result = select(nfds, &readfds, (fd_set*)NULL, (fd_set*)NULL, &tv);
//        if (result > 0)
//		{
//            DNSServiceErrorType err = kDNSServiceErr_NoError;
//            if (FD_ISSET(dns_sd_fd, &readfds))
//                err = DNSServiceProcessResult(serviceRef);
//            if (err) stopNow = true;
//		}
//        else
//		{
//            printf("select(  ) returned %d errno %d %s\n",
//				   result, errno, strerror(errno));
//            if (errno != EINTR) stopNow = true;
//		}
//	}
	
//	if (dns_sd_fd >= 0) {
//		shutdown(dns_sd_fd, SHUT_RDWR);
//		close(dns_sd_fd);
//	}




