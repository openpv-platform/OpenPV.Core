#include "AhsokaServices.h"
#include <iostream>
#include <string>

#include <QApplication>
#include "view.h"

int main(int argc, char *argv[])
{
    QApplication a(argc, argv);
    View w;
    w.setWindowFlags(Qt::Window | Qt::FramelessWindowHint);
    w.showFullScreen();
    w.show();
    AhsokaRuntime::ReleaseStartup();
    return a.exec();
}
