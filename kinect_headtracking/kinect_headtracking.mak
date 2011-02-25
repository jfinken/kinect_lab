OSTYPE := $(shell uname -s)

SRC_FILES = \
	../kinect_headtracking/KCircle.cpp \
	../kinect_headtracking/KGrid.cpp \
	../kinect_headtracking/KHeadTrack.cpp \
	../kinect_headtracking/KItems.cpp \
	../kinect_headtracking/KGlutInput.cpp \
	../kinect_headtracking/kKinect.cpp \
	../kinect_headtracking/KProgram.cpp \
	../kinect_headtracking/KVertex.cpp \
	../kinect_headtracking/main.cpp 

INC_DIRS += ../kinect_headtracking

EXE_NAME = kinect_headtracking 

DEFINES = USE_GLUT

ifeq ("$(OSTYPE)","Darwin")
        LDFLAGS += -framework OpenGL -framework GLUT
else
        USED_LIBS += glut
endif

include ../NiteSampleMakefile

