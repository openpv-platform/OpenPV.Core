#ifndef TYPE_C_ROLE_SWITCH_H
#define TYPE_C_ROLE_SWITCH_H

typedef enum {
    TYPE_C_ROLES_HOST,
    TYPE_C_ROLES_DEVICE,
} TYPE_C_ROLES_ENUM;

int type_c_role_switch(TYPE_C_ROLES_ENUM type_c_mode);

// TYPE_C_ROLE_SWITCH_H
#endif
