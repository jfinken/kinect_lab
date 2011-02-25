#pragma once
#include <cstdlib>
#ifdef USE_GLUT
#if (XN_PLATFORM == XN_PLATFORM_MACOSX)
        #include <GLUT/glut.h>
#else
        #include <GL/glut.h>
#endif
#else
#include "opengles.h"
#endif

// socket changes
#ifdef WIN32
#include <windows.h>
#include <winsock2.h>
#include <ws2tcpip.h>
#else
#include <netdb.h>
#include <arpa/inet.h>
#include <netinet/in.h>
#include <sys/socket.h>
#include <sys/types.h>
#endif

#include <stdio.h>
#include <stdlib.h>

//#include <gl/glut.h>
#include "defines.h"
#include "KHeadTrack.h"
#include "KGlutInput.h"
#include "kKinect.h"



class KProgram
{
	// This to be the main-helper-class and thus it should only be static, so no constructor
private:
	KProgram(void);
	~KProgram(void);

	// Saves the window-handle of the main window
	static int mWindowHandle;
    // socket stuff
    static void sendPosition(KVertex pos);
    static void connectSocket(void);
    static int sd;

public:
	// Sets up Glut for working
	static void initGlut(int argc, char* argv[]);
	static void glutDisplay(void);
	static void glutIdle(void);
	static void showWindow(void);
	static kKinect kinect;
	static KHeadTrack headtrack;

	static float x2;
	static float y2;
	static float z2;
};

