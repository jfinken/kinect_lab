#include "KProgram.h"

int KProgram::sd=0;
int KProgram::mWindowHandle = 0;
kKinect KProgram::kinect = kKinect();

/*
float KProgram::x2=0.9f;
float KProgram::y2=0.57f;
float KProgram::z2=2.55f;
*/
float KProgram::x2=1.1f;
float KProgram::y2=0.77f;
float KProgram::z2=2.55f;

KProgram::KProgram(void)
{
}

KProgram::~KProgram(void)
{
}

void KProgram::initGlut(int argc, char* argv[])
{
    // initialize socket	
    connectSocket();

	// Initalize Glut
	glutInit(&argc, argv);
	glutInitDisplayMode(GLUT_RGBA | GLUT_DOUBLE);

	// Set window size and position
	glutInitWindowSize(WINDOW_SIZE_X, WINDOW_SIZE_Y);
	glutInitWindowPosition(WINDOW_POS_X,WINDOW_POS_Y);

	// Create the window and save the handle
	KProgram::mWindowHandle = glutCreateWindow(WINDOW_TITLE);

	// Setting the Clear-Color
	glClearColor(WINDOW_CLEAR_COLOR);

	// GL-ENABLES:
	glEnable(GL_DEPTH_TEST);
	glEnable(GL_NORMALIZE);
	glEnable(GL_FOG);

	// Fog Configuration
	glFogi(GL_FOG_MODE, GL_LINEAR);

	// Setting the glut-funcs
	glutDisplayFunc(glutDisplay);
	glutIdleFunc(glutIdle);	
	glutMouseFunc(KGlutInput::glutMouse);
	glutKeyboardFunc(KGlutInput::glutKeyboard);
	glutMotionFunc(KGlutInput::glutMouseMotion);
}

KHeadTrack KProgram::headtrack(SCREEN_HEIGTH_MM);

void KProgram::glutDisplay(void)
{
	// First delete the old scene
	//glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
	
	// Then paint the new scene
	headtrack.renderScene();

	// Done painting --> now show result
	glutSwapBuffers();
}


void KProgram::glutIdle(void)
{
	// First do idle-stuff:
	KVertex position = kinect.getPosition();
    //printf("[glutIdle] kinect position: %f, %f, %f\n", position.mX, position.mY, position.mZ);
	headtrack.refreshData(position.mX,position.mY,position.mZ);
//	headtrack.refreshData(x2,y2,z2);

    // send data to socket server
    if(position.mX != 0.0 && position.mY != 0.0 && position.mZ != 0.0)
        sendPosition(position);

	// Done idle --> now repaint the window
	glutPostRedisplay();
}

void KProgram::showWindow(void){
	glutMainLoop();
}
//-----------------------------------------------------------------------------
// Connect to socket
//-----------------------------------------------------------------------------
void KProgram::connectSocket(void)
{
    int DEFAULT_PORT = 8124;
    char DEFAULT_HOST[] = {"127.0.0.1"};
    int DIRSIZE = 8192;

    char hostname[100];
    char dir[DIRSIZE];
    struct sockaddr_in sin;
    struct sockaddr_in pin;
    struct hostent *hp;

    strcpy(hostname,DEFAULT_HOST);

    // go find out about the desired host machine 
    if ((hp = gethostbyname(hostname)) == 0)
    {
        perror("gethostbyname");
        exit(1);
    }

    /*
    ZeroMemory( &hints, sizeof(hints) );
    hints.ai_family = AF_UNSPEC;
    hints.ai_socktype = SOCK_STREAM;
    hints.ai_protocol = IPPROTO_TCP;
    */

    // fill in the socket structure with host information 
    memset(&pin, 0, sizeof(pin));
    pin.sin_family = AF_INET;
    pin.sin_addr.s_addr = ((struct in_addr *)(hp->h_addr))->s_addr;
    pin.sin_port = htons(DEFAULT_PORT);

    // grab an Internet domain socket 
    if ((sd = socket(AF_INET, SOCK_STREAM, 0)) == -1) {
        perror("socket");
        exit(1);
    }

    // connect to PORT on HOST
    if (connect(sd,(struct sockaddr *)  &pin, sizeof(pin)) == -1) {
        perror("connect");
        exit(1);
    }
    else
        printf("[Client] Connected!\n");


}
//-----------------------------------------------------------------------------
// Send data to socket server
//-----------------------------------------------------------------------------
void KProgram::sendPosition(KVertex pos)
{
    // Allocate memory for the string we will send to the socket server.
    // length will be the size of the mem allocation for the string
    int length = snprintf(NULL,0 ,"%f,%f,%f", pos.mX, pos.mY, pos.mZ);

    // Character object that will store the string
    char * data = (char*) malloc((length + 1) * sizeof(char));
   
    // Print string in format of: [x,y,z]
    snprintf(data, length, "%f,%f,%f", pos.mX, pos.mY, pos.mZ);

    // Send data off to socket server
    //send( ConnectSocket, data, (length + 1), 0 );
    if (send(sd, data, (length + 1), 0) == -1) {
        perror("send");
        exit(1);
    }
   
    // Free up the memory used for the string
    free(data);
   
    // Print the same data to console.
    //printf("[socket send]: (%f,%f,%f)\n",pos.mX, pos.mY, pos.mZ);
}

