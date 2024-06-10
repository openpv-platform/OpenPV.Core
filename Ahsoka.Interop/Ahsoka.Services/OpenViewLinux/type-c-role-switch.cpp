#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/ioctl.h>
#include <unistd.h>

#include <stdint.h>

#include <linux/gpio.h>

#include "type-c-role-switch.h"

#define GPIO_INDEX  5
#define GPIO_DEV    "/dev/gpiochip"

#define CC_RD_EN_PIN 7
#define CC_RP_EN_PIN 9

#define FILE_DESC_HOST_ROLE             "host"
#define FILE_DESC_HOST_ROLE_COUNT       4

#define FILE_DESC_PERIPHERAL_ROLE       "none"
#define FILE_DESC_PERIPHERAL_ROLE_COUNT 4

static int open_gpio_file_desc(int *file_desc, int gpiochip_index) {
    char gpio_dev_name[20];
    int ret = 0;

    // Store the name of the gpio device in the chr
    sprintf(gpio_dev_name, "%s%d", GPIO_DEV, gpiochip_index);

    // Open the file descriptor for the device
    *file_desc = open(gpio_dev_name, 0);
    if (*file_desc == -1) {
        // If the file descriptor wasn't successfully able to be opened, return an error
        ret = -errno;
        fprintf(stderr, "Failed to open the file descriptor: %s\n", gpio_dev_name);

        return ret;
    }

    return ret;
}

static int open_pin_linehandle(struct gpiohandle_request *req, struct gpiohandle_data *data, int *file_desc, int pin_index) {
    int ret = 0;

    // Request the GPIO linehandle for the given pin index
    req->lineoffsets[0] = pin_index;
    req->flags = GPIOHANDLE_REQUEST_OUTPUT;
    memcpy(req->default_values, data, sizeof(req->default_values));
    // strcpy(req.consumer_label, "led_gpio_a_14");
    req->lines = 1;

    ret = ioctl(*file_desc, GPIO_GET_LINEHANDLE_IOCTL, req);
    if (ret == -1) {
        ret = -errno;
        fprintf(stderr, "Failed to issue GET LINEHANDLE IOCTL (%d)\n", ret);
    }

    return ret;
}

static int close_pin_linehandle(struct gpiohandle_request *req) {
    int ret = 0;

    ret = close(req->fd);
    if (ret == -1) {
        perror("Failed to close GPIO LINEHANDLE device file.");
        ret = -errno;
    }

    return ret;
}

static int set_pin_value(struct gpiohandle_request *req, struct gpiohandle_data *data, uint8_t value) {
    int ret = 0;

    data->values[0] = value;
    ret = ioctl(req->fd, GPIOHANDLE_SET_LINE_VALUES_IOCTL, data);
    if (ret == -1) {
        ret = -errno;
        fprintf(stderr, "Failed to issue %s (%d)\n",
                "GPIOHANDLE_SET_LINE_VALUES_IOCTL", ret);
    }

    return ret;
}

#define IF_RET      \
    if (ret != 0) { \
        return ret; \
    }

int type_c_role_switch(TYPE_C_ROLES_ENUM type_c_mode) {
    int ret = 0;

    char usb_role_write[10];
    int usb_role_write_count = 0;
    uint8_t gpio_set_value;
    switch (type_c_mode) {
    case TYPE_C_ROLES_HOST:
        sprintf(usb_role_write, FILE_DESC_HOST_ROLE);
        usb_role_write_count = FILE_DESC_HOST_ROLE_COUNT;
        gpio_set_value = 0;
        break;
    case TYPE_C_ROLES_DEVICE:
        sprintf(usb_role_write, FILE_DESC_PERIPHERAL_ROLE);
        usb_role_write_count = FILE_DESC_PERIPHERAL_ROLE_COUNT;
        gpio_set_value = 1;
        break;
    default:
        ret = -errno;
        perror("An unrecognized usb role was requested");
        return ret;
    }

    //////////////////////////////////////////////////
    // GPIO setup
    //////////////////////////////////////////////////
    struct gpiohandle_request req_rd, req_rp;
    struct gpiohandle_data data_rd, data_rp;
    int file_desc_gpio;

    // Open the file descript for the gpiochip that contains the two pins that we are going to be modifying
    ret = open_gpio_file_desc(&file_desc_gpio, GPIO_INDEX);
    IF_RET;

    // open up the linehandles for the two pins that we are going to want to modify
    ret = open_pin_linehandle(&req_rd, &data_rd, &file_desc_gpio, CC_RD_EN_PIN);
    IF_RET;
    ret = open_pin_linehandle(&req_rp, &data_rp, &file_desc_gpio, CC_RP_EN_PIN);
    IF_RET;

    //////////////////////////////////////////////////
    // usb role setup
    //////////////////////////////////////////////////
    // Open the file descriptor for the usb role to be able to change it
    char usb_role_dev_name[] = "/sys/devices/platform/soc/49000000.usb-otg/usb_role/49000000.usb-otg-role-switch/role";
    int file_desc_usb_role;
    file_desc_usb_role = open(usb_role_dev_name, O_WRONLY);

    //////////////////////////////////////////////////
    // Logic
    //////////////////////////////////////////////////
    int write_count = write(file_desc_usb_role, usb_role_write, usb_role_write_count);
    if (write_count != usb_role_write_count) {
        ret =-errno;
        perror("The usb role file descriptor was unsuccessfully written to.");
        return ret;
    }

    ret = set_pin_value(&req_rd, &data_rd, gpio_set_value);
    IF_RET;
    ret = set_pin_value(&req_rp, &data_rp, gpio_set_value);
    IF_RET;

    //////////////////////////////////////////////////
    // cleanup
    //////////////////////////////////////////////////
    // close the linehandles for the two pins that we were modifying
    ret = close_pin_linehandle(&req_rd);
    IF_RET;
    ret = close_pin_linehandle(&req_rp);
    IF_RET;

    return ret;
}
