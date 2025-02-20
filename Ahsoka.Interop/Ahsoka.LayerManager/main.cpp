#include <stdio.h>
#include <stdlib.h>
#include "LayerManagerService.h"

int main (int argc, char *argv[])
{
	cout << "LayerManager: Startup Ready " << std::endl;
  	AhsokaLayerManager::LayerManagerService service;
	service.StartAndRun();

	return 0;
}
