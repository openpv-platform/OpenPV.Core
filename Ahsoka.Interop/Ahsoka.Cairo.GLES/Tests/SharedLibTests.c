// C++ level context implementation test
#include <stdio.h>
#include <stdlib.h>
#include "../OpenPVGLESContext.h"
#include "../Wayland/xdg-shell-client-protocol.h"



// Tests Prototypes:
cairo_t *Test_CairoContext();
cairo_surface_t *Test_CairoSurface();


int main()
{
	int ret;
	ret = InitWindowInterop(0, 0, 300, 400, false);
	if (ret != 0)
	{
		printf("Error Initializing Wayland Client! Exiting Test...\n\r");
		exit(-1);
	}

	cairo_t *ctx = Test_CairoContext();
	if (!ctx)
	{
		printf("Error getting Cairo Context! Exiting Test...\n\r");
		exit(-1);
	}

	cairo_surface_t *surf = Test_CairoSurface();
	if (!surf)
	{
		printf("Error getting Cairo Surface! Exiting Test...\n\r");
		exit(-1);
	}

	printf("Initial Software Tests Passed, Drawing on screen for physical inspection...\n\r");

	// Visual Tests moved to Ahsoka.Demo.Cairo project
    return 0;
}


cairo_t* Test_CairoContext()
{
	return GetCairoContext();
}

cairo_surface_t* Test_CairoSurface()
{
	return GetCairoSurface();
}


