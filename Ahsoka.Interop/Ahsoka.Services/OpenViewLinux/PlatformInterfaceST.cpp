#include <PlatformInterface.h>
#include <type-c-role-switch.h>

#include <iostream>

void SetPlatformUsbRoleMode(int mode)
{	
	int returnCode = 0;
	if (0 == mode)
		returnCode = type_c_role_switch(TYPE_C_ROLES_HOST);
	else if (1 == mode)
		returnCode = type_c_role_switch(TYPE_C_ROLES_DEVICE);
	
	if (returnCode != 0)
		std::cout << "Ahsoka.Services (ST): SetPlatformUsbRoleMode failed: " << returnCode << std::endl;
	else
		std::cout << "Ahsoka.Services (ST): SetPlatformUsbRoleMode succeeded" << std::endl;
}