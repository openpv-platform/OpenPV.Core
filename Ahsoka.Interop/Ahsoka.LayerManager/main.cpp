#include <stdio.h>
#include <stdlib.h>
#include "LayerManager.h"

int main (int argc, char *argv[])
{

	// Init Mutex and ILM Interface
    Init();

	// Get Screen Info (Single Screen Support Only)
	GetScreenInfo();

	// Create Default Screen Layers
	CreateScreenLayers();

    // Run Main App Loop
    AppRun();

    return 0;
}
