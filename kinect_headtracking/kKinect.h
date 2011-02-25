#pragma once
#include <XnCppWrapper.h>
#include <iostream>
#include <string>
#include "KVertex.h"
#include "defines.h"
#include <stdio.h>

class kKinect
{
public:
	kKinect(void);
	~kKinect(void);
	KVertex getPosition(void);
private:
	xn::Context mContext;
	xn::DepthGenerator mDepth;
	xn::ImageGenerator mImage;
	xn::UserGenerator user;
	xn::SkeletonCapability* skeleton;
	XnUserID pUser[1];
	XnUInt16 nUsers;

	void initKinect(void);
	bool checkError(std::string message, XnStatus RetVal);

    //jf
    //void XN_CALLBACK_TYPE CalibrationStarted(xn::SkeletonCapability& skeleton, 
     //                                   XnUserID user, void* cxt);
    //void XN_CALLBACK_TYPE CalibrationEnded(xn::SkeletonCapability& skeleton, 
                                    //XnUserID user, XnBool bSuccess, void* cxt);
public:
	void reset(void);
	void calibrateUser();
};

