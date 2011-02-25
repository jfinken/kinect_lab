#include "kKinect.h"
#include "KProgram.h"

void XN_CALLBACK_TYPE CalibrationStarted(xn::SkeletonCapability& skeleton,                                         XnUserID user, void* cxt);
void XN_CALLBACK_TYPE CalibrationEnded(xn::SkeletonCapability& skeleton, 
                                                XnUserID user, XnBool bSuccess, void* cxt);


kKinect::kKinect(void)
{
	initKinect();
	pUser[0] = 0;
	nUsers = 1;
	skeleton = 0;
}


kKinect::~kKinect(void)
{
	if(skeleton!=0) 
		delete skeleton;
}


void kKinect::initKinect(void)
{

	XnStatus nRetVal;
	nRetVal = XN_STATUS_OK;

	/* Context initialisieren (Kameradaten) */
	nRetVal = mContext.Init();
	if(!checkError("Fehler beim Initialisieren des Context", nRetVal))
        exit(1);


	/* Tiefengenerator erstellen */
	nRetVal = mDepth.Create(mContext);
	if(!checkError("Fehler beim Erstellen des Tiefengenerators", nRetVal))
        exit(1);

	/* Tiefengenerator einstellen */
	XnMapOutputMode outputModeDepth;
	outputModeDepth.nXRes = 640;
	outputModeDepth.nYRes = 480;
	outputModeDepth.nFPS = 30;
	nRetVal = mDepth.SetMapOutputMode(outputModeDepth);
	if(!checkError("Fehler beim Konfigurieren des Tiefengenerators", nRetVal))
        exit(1);


	/* Imagegenerator erstellen */
	nRetVal = mImage.Create(mContext);
	if(!checkError("Fehler beim Erstellen des Bildgenerators", nRetVal))
        exit(1);

	/* Imagegenerator einstellen */
	XnMapOutputMode outputModeImage;
	outputModeImage.nXRes = 640;
	outputModeImage.nYRes = 480;
	outputModeImage.nFPS = 30;
	nRetVal = mImage.SetMapOutputMode(outputModeImage);
	if(!checkError("Fehler beim Konfigurieren des Bildgenerators", nRetVal))
        exit(1);

	/* SceneAnalzer einstellen */
	nRetVal = user.Create(mContext);
	if(!checkError("Fehler beim Konfigurieren des Usergenerators", nRetVal))
        exit(1);

	/* Starten der Generatoren - volle Kraft vorraus! */
	nRetVal = mContext.StartGeneratingAll();
	if(!checkError("Fehler beim Starten der Generatoren", nRetVal))
        exit(1);
}


bool kKinect::checkError(std::string message, XnStatus RetVal)
{
	if(RetVal != XN_STATUS_OK) {
		std::cout << message << ": " << xnGetStatusString(RetVal) << std::endl;
		return false;
	}
	return true;
}


KVertex kKinect::getPosition(void)
{
	mContext.WaitAndUpdateAll();
	/* Anzahl der User auslesen und in Objekten speichern */
    // number of users read and save to objects
	nUsers=1;
	user.GetUsers(pUser, nUsers);
	if(nUsers>0) 
    {
		/* User dem Skeleton zuweisen */
        // assign users to skeleton
		xn::SkeletonCapability pSkeleton = user.GetSkeletonCap();
		if(skeleton!=0) 
        {
			delete skeleton;
			skeleton = 0;
		}
		skeleton=new xn::SkeletonCapability(pSkeleton);
		if(skeleton->IsCalibrated(pUser[0])) 
        {
			/* Alle Körperteile auswählen */
            // select all parts of the body
			skeleton->SetSkeletonProfile(XN_SKEL_PROFILE_ALL);
	
			/* Kopf initialisieren */
            // head initialize
			XnSkeletonJointTransformation head;
			skeleton->StartTracking(pUser[0]);
			skeleton->GetSkeletonJoint(pUser[0], XN_SKEL_HEAD, head);

			if(head.position.fConfidence && head.orientation.fConfidence) 
            {
                /*
				std::cout << "x: " << head.position.position.X << ", " <<
						"y: " << head.position.position.Y << ", " <<
                        "z: " << head.position.position.Z << "    " <<
                        "[x*x2]: "<<(head.position.position.X*KProgram::x2) <<", "<<
                        "[-y*y2]: "<<(-(head.position.position.Y+200.0)*KProgram::y2)<<", "<<
                        "[z*z2]: "<<(head.position.position.Z*KProgram::z2) << std::endl;
                */
								//"x2: " << head.position.position.X/SCREEN_HEIGTH_MM <<
								//"y2: " << (head.position.position.Y+200.0)/SCREEN_HEIGTH_MM <<
								//std::endl;
				return KVertex(head.position.position.X*KProgram::x2, 
                               -(head.position.position.Y+200.0)*KProgram::y2, 
                               head.position.position.Z*KProgram::z2, KRGBColor());
			}
		}
        //else
            //printf("[kKinect] skeleton->IsCalibrated for user 0 returning false...\n");
	}
    //else
    //    printf("[kKinect] nUsers is 0, sorry sucker. Or hit 'c'[Enter] to calibrate a user...\n");
	return KVertex();
}


void kKinect::reset(void)
{
	if(pUser[0]!=0) {
		user.GetSkeletonCap().Reset(pUser[0]);
		pUser[0]=0;
		nUsers=1;
		if(skeleton!=0) {
			delete skeleton;
			skeleton = 0;
		}
	}
}


void kKinect::calibrateUser()
{	
    std::cout << "About to calibrate user..." << std::endl;
    XnCallbackHandle hCalibrationCBs;
	mContext.WaitAndUpdateAll();
	if(skeleton==0) 
    {
		xn::SkeletonCapability pSkeleton = user.GetSkeletonCap();

		skeleton=new xn::SkeletonCapability(pSkeleton);
        
        skeleton->RegisterCalibrationCallbacks(CalibrationStarted, 
                                               CalibrationEnded, NULL, hCalibrationCBs);
	}
	if(user.GetNumberOfUsers() != 0) 
    {
		XnStatus status = skeleton->RequestCalibration(pUser[0],true);
		//std::cout << "Kalibration wird gestartet, bitte Arme im 90 Grad Winkel nach oben halten." << std::endl;
        //std::cout << "Calibration is started, keep your arms at a 90 degree angle to the top" << std::endl;
        if(status != XN_STATUS_OK)
            printf("Calibration status is NOT OK: %d\n", status);
	}
    else
        std::cout << "No users detected..." << std::endl;
}
void XN_CALLBACK_TYPE CalibrationStarted(xn::SkeletonCapability& skeleton, XnUserID user, void* cxt)
{
        printf("Calibration started.  Keep arms at 90 degrees to the top...\n");
}
void XN_CALLBACK_TYPE CalibrationEnded(xn::SkeletonCapability& skeleton, XnUserID user, XnBool bSuccess, void* cxt)
{
        printf("Calibration done [user: %d] %ssuccessfully...\n", user, bSuccess?"":"un");
}
